using System;
using System.Collections.Generic;
using AssetManagement;
using Fusumity.Utility;
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

		protected internal UIWidget Anchor { get; }
		internal void Bind(UIWidget anchor);
		internal void Unbind();
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
		protected internal UIWidget _anchor;

		private const string LAYOUT_PREFIX_NAME = "[Popover] ";

		private UIPopoverEntry _entry;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		string IIdentifiable.Id => Id;

		private event Action<IPopover> RequestedClose;

		event Action<IPopover> IPopover.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		UIWidget IPopover.Anchor => _anchor;

		protected override string Layer
		{
			get
			{
				if (_layout)
					return _anchor?.Layer ?? LayerType.POPOVERS;

				return LayerType.POPOVERS;
			}
		}

		protected override ComponentReferenceEntry LayoutReference => _entry.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _entry.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _entry.layout.preloadAssets;

		private TransformSnapshot _layoutTransformSnapshot;

		public sealed override void SetupLayout(TLayout layout)
		{
			_layoutTransformSnapshot = layout.transform
			   .Snapshot(TransformSpace.Local);
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
			_layout.transform.Apply(in _layoutTransformSnapshot);

			base.OnLayoutInstalledInternal();
		}

		void IPopover.Bind(UIWidget anchor)
		{
			_anchor = anchor;

			UpdateParentTransformBindSafe();
			if (_layout)
				_layout.transform.Apply(in _layoutTransformSnapshot);
		}

		void IPopover.Unbind()
		{
			_anchor = null;

			UpdateParentTransformBindSafe();
			SetActive(false, true);
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

			SetActive(true, Suppress);
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

			var parentRectTransform = _anchor?.RectTransform ?? UIDispatcher.Get(Layer).rectTransform;
			_layout.transform.SetParent(parentRectTransform, false);
		}
	}

	public struct EmptyPopoverArgs : IPopoverArgs
	{
	}

	public interface IPopoverArgs
	{
	}
}
