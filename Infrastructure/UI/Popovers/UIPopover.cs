using System;
using System.Collections.Generic;
using AssetManagement;
using Fusumity.Utility;
using JetBrains.Annotations;
using Sapientia;
using Sapientia.Extensions;
using UI.Layers;
using UnityEngine;

namespace UI.Popovers
{
	public interface IPopover : IWidget, IIdentifiable
	{
		public void RequestClose();

		internal event Action<IPopover> RequestedClose;

		internal void Initialize(in UIPopoverEntry entry);

		internal void Show(IPopoverArgs args);

		protected internal UIWidget Host { get; }
		internal void Attach(UIWidget parent, RectTransform customAnchor = null);
		internal void Detach();
	}

	public abstract class UIPopover<TLayout> : UIBasePopover<TLayout, EmptyPopoverArgs>
		where TLayout : UIBasePopoverLayout
	{
	}

	public abstract class UIPopover<TLayout, TArgs> : UIBasePopover<TLayout, TArgs>
		where TLayout : UIBasePopoverLayout
		where TArgs : struct, IPopoverArgs
	{
		protected sealed override void OnShow() => OnShow(ref _args);

		protected virtual void OnShow(ref TArgs args)
		{
		}

		protected sealed override void OnHide() => OnHide(ref _args);

		protected virtual void OnHide(ref TArgs args)
		{
		}
	}

	public abstract class UIBasePopover<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IPopover
		where TLayout : UIBasePopoverLayout
		where TArgs : struct, IPopoverArgs
	{
		protected internal UIWidget _host;

		[CanBeNull]
		protected internal RectTransform _customAnchor;

		private const string LAYOUT_PREFIX_NAME = "[Popover] ";

		private UIPopoverEntry _entry;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		private bool _layoutResetRequest;

		string IIdentifiable.Id => Id;

		private event Action<IPopover> RequestedClose;

		event Action<IPopover> IPopover.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		UIWidget IPopover.Host => _host;

		protected override string Layer
		{
			get
			{
				if (_layout)
					return _host?.Layer ?? LayerType.POPOVERS;

				return LayerType.POPOVERS;
			}
		}

		protected override ComponentReferenceEntry LayoutReference => _entry.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _entry.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _entry.layout.preloadAssets;

		public sealed override void SetupLayout(TLayout layout)
		{
			OnSetupDefaultAnimator();
			base.SetupLayout(layout);
		}

		[Obsolete(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE, true)]
		public sealed override void Initialize() =>
			throw new Exception(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT.Format(GetType().Name));

		void IPopover.Initialize(in UIPopoverEntry entry)
		{
			_entry = entry;
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

		void IPopover.Attach(UIWidget host, RectTransform customAnchor = null)
		{
			_host = host;
			_customAnchor = customAnchor;

			UpdateParentTransformBindSafe();
			if (_layout)
				_layout.ResetTransform();
			else
				_layoutResetRequest = true;
		}

		void IPopover.Detach()
		{
			_host = null;
			_customAnchor = null;

			UpdateParentTransformBindSafe();
			SetActive(false, true, false);
		}

		void IPopover.Show(IPopoverArgs boxedArgs)
		{
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
			SetActive(true, suppressAnyFlag);
			DisableSuppress();
		}

		protected sealed override void OnEndedClosingInternal()
		{
			if (_resetting.HasValue)
			{
				if (_resetting.Value)
					Reset(false);

				_resetting = null;
			}

			base.OnEndedClosingInternal();
		}

		public override void RequestClose() => RequestedClose?.Invoke(this);

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			if (_animator == null)
				SetAnimator<DefaultPopoverAnimator<UIBasePopover<TLayout, TArgs>>>();
		}

		private TArgs UnboxedArgs(IPopoverArgs boxedArgs)
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

			var parentRectTransform = _customAnchor;
			if (!_customAnchor)
				parentRectTransform = _host?.RectTransform ?? UIDispatcher.GetLayer(Layer).rectTransform;

			_layout.transform
			   .SetParent(parentRectTransform, false);
		}
	}

	public struct EmptyPopoverArgs : IPopoverArgs
	{
	}

	public interface IPopoverArgs
	{
	}
}
