using System;
using System.Collections.Generic;
using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using UI.Layers;
using UnityEngine;

namespace UI.Popovers
{
	public interface IPopover : IWidget, IIdentifiable
	{
		protected internal RectTransform Anchor { get; }

		void RequestClose();
		Type GetArgsType();
		internal object GetArgs();

		internal void Initialize(in UIPopoverConfig config);

		internal void Show(object args, bool immediate);

		internal void Attach(RectTransform anchor);
		internal void Detach();

		internal void UpdateAnchor(RectTransform anchor);

		internal event Action<IPopover> RequestedClose;

		UniTask WaitOpeningAsync(CancellationToken? cancellationToken = null);
		UniTask WaitClosingAsync(CancellationToken? cancellationToken = null);
	}

	public abstract class UIPopover<TLayout> : UIBasePopover<TLayout, EmptyArgs>
		where TLayout : UIBasePopoverLayout
	{
	}

	public abstract class UIPopover<TLayout, TArgs> : UIBasePopover<TLayout, TArgs>
		where TLayout : UIBasePopoverLayout
	{
		private bool _suppressHide;

		protected sealed override void OnShow()
		{
			if (_args is ICloseRequestor requestor)
				requestor.CloseRequested += RequestCloseInternal;

			OnShow(ref _args);
		}

		protected abstract void OnShow(ref TArgs args);

		protected sealed override void OnHide()
		{
			if (_args is ICloseRequestor requestor)
				requestor.CloseRequested -= RequestCloseInternal;

			if (_suppressHide)
				return;

			OnHide(ref _args);
		}

		protected virtual void OnHide(ref TArgs args)
		{
		}

		protected override void OnBeforeSetupTemplate()
		{
			if (typeof(TArgs) == typeof(EmptyArgs))
			{
				_suppressHide = !Active;
				return;
			}

			_suppressHide = _args == null;
		}

		protected override void OnAfterSetupTemplate() => _suppressHide = false;
	}

	public abstract class UIBasePopover<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IPopover
		where TLayout : UIBasePopoverLayout
	{
		protected internal RectTransform _anchor;

		private const string LAYOUT_PREFIX_NAME = "[Popover] ";

		private UIPopoverConfig _config;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		private bool _layoutResetRequest;

		string IIdentifiable.Id => Id;

		public ref TArgs ViewModel => ref _args;

		private event Action<IPopover> RequestedClose;

		event Action<IPopover> IPopover.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		RectTransform IPopover.Anchor => _anchor;

		protected override string Layer => LayerType.POPOVERS;

		protected override ComponentReferenceEntry LayoutReference => _config.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _config.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _config.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _config.layout.preloadAssets;

		public sealed override void SetupLayout(TLayout layout)
		{
			OnSetupDefaultAnimator();
			base.SetupLayout(layout);
		}

		[Obsolete(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE, true)]
		public sealed override void Initialize() =>
			throw new Exception(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT.Format(GetType().Name));

		void IPopover.Initialize(in UIPopoverConfig config)
		{
			_config = config;
			base.Initialize();
		}

		protected override void OnLayoutInstalledInternal()
		{
			UpdateParentTransformBindSafe();

			if (_layoutResetRequest)
			{
				_layout.ResetTransform();
				_layoutResetRequest = false;
			}

			base.OnLayoutInstalledInternal();
		}

		void IPopover.Attach(RectTransform anchor)
		{
			UpdateAnchorInternal(anchor);
		}

		void IPopover.Detach()
		{
			ClearAnchor();
		}

		void IPopover.UpdateAnchor(RectTransform anchor)
		{
			UpdateAnchorInternal(anchor);
		}

		private void UpdateAnchorInternal(RectTransform anchor)
		{
			if (anchor == null)
			{
				ClearAnchor();
				return;
			}

			if (_anchor == anchor)
				return;

			_anchor = anchor;

			UpdateParentTransformBindSafe();
			if (_layout)
				_layout.ResetTransform();
			else
				_layoutResetRequest = true;
		}

		private void ClearAnchor()
		{
			_anchor = null;

			UpdateParentTransformBindSafe();
			SetActive(false, true, false);
			Reset(false);
		}

		void IPopover.Show(object boxedArgs, bool immediate)
		{
			TryResetInternal();

			if (boxedArgs != null)
			{
				var args = UnboxedArgs(boxedArgs);

				if (Active)
				{
					if (_args.Equals(args))
						return;

					EnableSuppress();

					// Неявное поведение...
					// Нужно вызывать OnHide у попапа если хотим
					// переоткрыть тот же попап с новыми аргументами
					SetActive(false, true, false);
				}

				_args = args;
			}

			var suppressAnyFlag = suppressFlag != SuppressFlag.None;
			SetActive(true, suppressAnyFlag || immediate);
			DisableSuppress();
		}

		protected sealed override void OnEndedClosingInternal()
		{
			TryResetInternal();

			base.OnEndedClosingInternal();
		}

		public Type GetArgsType() => typeof(TArgs);

		object IPopover.GetArgs() => _args;

		public override void RequestClose()
		{
			if (_args is ICloseInterceptor interceptor)
			{
				interceptor.RequestClose();
				return;
			}

			RequestCloseInternal();
		}

		private protected void RequestCloseInternal()
		{
			RequestedClose?.Invoke(this);
		}

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			if (_animator == null)
				SetAnimator<DefaultPopoverAnimator<TLayout, UIBasePopover<TLayout, TArgs>>>();
		}

		private TArgs UnboxedArgs(object boxedArgs)
		{
			if (boxedArgs == null)
				throw GUIDebug.NullException($"Passed null args ({typeof(TArgs)}) to popup of type [{GetType()}]");

			if (boxedArgs is not TArgs args)
				throw GUIDebug.Exception($"Passed wrong args ({boxedArgs.GetType()}) to popup of type " +
					$"[{GetType()}] (need type: {typeof(TArgs)})");

			return args;
		}

		private void UpdateParentTransformBindSafe()
		{
			if (!_layout)
				return;

			var parent = _anchor != null
				? _anchor
				: UIDispatcher.GetLayer(Layer).rectTransform;
			_layout.transform.SetParent(parent, false);
		}

		private void TryResetInternal()
		{
			if (!_resetting.TryGetValue(out var resetting))
				return;

			if (resetting)
				Reset(false);

			_resetting = null;
		}
	}
}
