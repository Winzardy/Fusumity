using System;
using System.Collections.Generic;
using AssetManagement;
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

		internal void Bind(UIWidget parent);
		internal void Clear();
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
		protected internal UIWidget _source;

		private const string LAYOUT_PREFIX_NAME = "[Popover] ";

		private UIPopoverEntry _entry;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		string IIdentifiable.Id => Id;

		private event Action<IPopover> RequestedClose;

		event Action<IPopover> IPopover.RequestedClose
		{
			add => RequestedClose += value;
			remove => RequestedClose -= value;
		}

		protected override string Layer => _source?.Layer ?? LayerType.POPOVERS;

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
			RebindTransformSafe();
			base.OnLayoutInstalledInternal();
		}

		void IPopover.Bind(UIWidget source)
		{
			_source = source;
			RebindTransformSafe();
		}

		void IPopover.Clear()
		{
			_source = null;
			RebindTransformSafe();
		}

		private void RebindTransformSafe()
		{
			if (!_layout)
				return;

			var parentRectTransform = _source?.RectTransform ?? UIDispatcher.Get(Layer).rectTransform;
			_layout.transform.SetParent(parentRectTransform, false);
		}

		void IPopover.Show(IPopoverArgs boxedArgs)
		{
			var force = false;

			if (boxedArgs != null)
			{
				var args = UnboxedArgs(boxedArgs);

				if (Active)
				{
					if (_args.Equals(args))
						return;

					force = true;

					// Неявное поведение...
					// Нужно вызывать OnHide у попапа если хотим
					// переоткрыть тот же попап с новыми аргументами
					_suppressShownOrHiddenEvents = true;
					SetActive(false, true);
				}

				_args = args;
			}

			SetActive(true, force);
			_suppressShownOrHiddenEvents = false;
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
				SetAnimator<DefaultPopoverAnimator>();
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
	}

	public struct EmptyPopoverArgs : IPopoverArgs
	{
	}

	public interface IPopoverArgs
	{
	}
}
