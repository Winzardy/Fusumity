using System;
using System.Collections.Generic;
using AssetManagement;
using Content.Constants.UI.Layers;
using Sapientia;
using Sapientia.Extensions;
using Unity.Collections.LowLevel.Unsafe;

namespace UI.Popovers
{
	public interface IPopover : IIdentifiable, IDisposable
	{
		public bool Active { get; }
		public bool Visible { get; }

		public void RequestClose();

		internal event Action<IPopover> RequestedClose;
		internal void Initialize(UIPopoverEntry entry);
		internal void Show(ref IPopoverArgs args);
		internal void Hide();
		internal ref IPopoverArgs GetArgs();
	}

	public abstract class UIPopover<TLayout> : UIBasePopover<TLayout, DefaultPopoverArgs>, IPopover
		where TLayout : UIBaseLayout
	{
	}

	public abstract class UIPopover<TLayout, TArgs> : UIBasePopover<TLayout, TArgs>
		where TLayout : UIBaseLayout
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
		where TLayout : UIBaseLayout
		where TArgs : struct, IPopoverArgs
	{
		private const string LAYOUT_PREFIX_NAME = "[Popover] ";

		protected UIPopoverEntry _entry;

		protected TArgs _args;

		string IIdentifiable.Id => Id;

		private event Action<IPopover> RequestedClose;

		event Action<IPopover> IPopover.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		protected override string Layer => LayerType.POPUPS;
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

		void IPopover.Initialize(UIPopoverEntry entry)
		{
			_entry = entry;

			base.Initialize();
		}

		void IPopover.Show(ref IPopoverArgs boxedArgs)
		{
			var force = false;

			if (boxedArgs != null)
			{
				if (boxedArgs is not TArgs args)
					throw new Exception($"Passed null args ({typeof(TArgs)}) to popup of type: [{GetType()}]");

				if (Active)
				{
					if (_args.Equals(args))
						return;

					force = true;

					//Неявное поведение...
					//Нужно вызывать OnHide у попапа если хотим
					//переоткрыть тот же попап с новыми аргументами
					SetActive(false, true);
				}

				_args = args;
			}

			SetActive(true, force);
		}

		void IPopover.Hide() => SetActive(false);

		protected sealed override void OnEndedClosingInternal()
		{
			_args = default;
			base.OnEndedClosingInternal();
		}

		ref IPopoverArgs IPopover.GetArgs() =>
			ref UnsafeUtility.As<TArgs, IPopoverArgs>(ref _args);

		public override void RequestClose() => RequestedClose?.Invoke(this);

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			//if (_animator == null)
			//SetAnimator<DefaultPopupAnimator>();
		}

		protected sealed override void OnLayoutInstalledInternal()
		{
			// if(_layout.close)
			// 	_layout.close.Subscribe(OnCloseClicked);

			base.OnLayoutInstalledInternal();
		}

		protected sealed override void OnLayoutClearedInternal()
		{
			// if(_layout.close)
			// 	_layout.close.Unsubscribe(OnCloseClicked);

			base.OnLayoutClearedInternal();
		}

		protected virtual void OnCloseClicked() => RequestClose();
	}

	public struct DefaultPopoverArgs : IPopoverArgs
	{
	}

	public interface IPopoverArgs
	{
	}
}
