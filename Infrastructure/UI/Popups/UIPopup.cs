using System;
using System.Collections.Generic;
using AssetManagement;
using Sapientia;
using Sapientia.Extensions;
using UI.Layers;

namespace UI.Popups
{
	public interface IPopup : IWidget, IIdentifiable
	{
		public void RequestClose();

		internal event Action<IPopup> RequestedClose;

		internal void Initialize(in UIPopupEntry entry);

		/// <param name="args">Важно подметить, что аргументы могут быть изменены в процессе использования попапа!
		/// При запросе открытия окна аргументы копируются в окно, далее могут измениться и эти аргументы система может
		/// получить через GetArgs() в своих целях (скрыть попап на время).
		/// Получается аргументы задают состояние окна от начала до конца использования.</param>
		internal void Show(IPopupArgs args);

		//TODO: поймал кейс в котором окно для корника осталось в очереди, потому что его никто не закрыл (PauseWindow)
		internal bool CanShow(IPopupArgs args, out string error);

		internal void Hide(bool reset);
		internal IPopupArgs GetArgs();
	}

	public abstract class UIPopup<TLayout> : UIBasePopup<TLayout, EmptyPopupArgs>, IPopup
		where TLayout : UIBasePopupLayout
	{
		private protected sealed override IPopupArgs GetArgs() => null;

		protected sealed override bool CanShow(ref EmptyPopupArgs _, out string error) => CanShow(out error);

		protected virtual bool CanShow(out string error)
		{
			error = string.Empty;
			return true;
		}
	}

	public abstract class UIPopup<TLayout, TArgs> : UIBasePopup<TLayout, TArgs>
		where TLayout : UIBasePopupLayout
		where TArgs : struct, IPopupArgs
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

	public abstract class UIBasePopup<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IPopup
		where TLayout : UIBasePopupLayout
		where TArgs : struct, IPopupArgs
	{
		private const string LAYOUT_PREFIX_NAME = "[Popup] ";

		private UIPopupEntry _entry;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		string IIdentifiable.Id => Id;

		private event Action<IPopup> RequestedClose;

		event Action<IPopup> IPopup.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

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

		void IPopup.Initialize(in UIPopupEntry entry)
		{
			_entry = entry;

			base.Initialize();
		}

		void IPopup.Show(IPopupArgs boxedArgs)
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

		bool IPopup.CanShow(IPopupArgs boxedArgs, out string error)
		{
			var args = UnboxedArgs(boxedArgs);
			return CanShow(ref args, out error);
		}

		void IPopup.Hide(bool reset)
		{
			_resetting = reset;
			SetActive(false);
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

		IPopupArgs IPopup.GetArgs() => GetArgs();

		private protected virtual IPopupArgs GetArgs() => _args;

		public override void RequestClose() => RequestedClose?.Invoke(this);

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			if (_animator == null)
				SetAnimator<DefaultPopupAnimator>();
		}

		protected sealed override void OnLayoutInstalledInternal()
		{
			if (_layout.close)
				_layout.close.Subscribe(OnCloseClicked);

			base.OnLayoutInstalledInternal();
		}

		protected sealed override void OnLayoutClearedInternal()
		{
			if (_layout.close)
				_layout.close.Unsubscribe(OnCloseClicked);

			base.OnLayoutClearedInternal();
		}

		protected virtual void OnCloseClicked() => RequestClose();

		private TArgs UnboxedArgs(IPopupArgs boxedArgs)
		{
			if (boxedArgs == null)
				throw new ArgumentException($"Passed null args ({typeof(TArgs)}) to popup of type [{GetType()}]");

			if (boxedArgs is not TArgs args)
				throw new Exception(
					$"Passed wrong args ({boxedArgs.GetType()}) to popup of type [{GetType()}] (need type: {typeof(TArgs)})");

			return args;
		}

		protected virtual bool CanShow(ref TArgs args, out string error)
		{
			error = string.Empty;
			return true;
		}
	}

	public struct EmptyPopupArgs : IPopupArgs
	{
	}

	public interface IPopupArgs
	{
	}
}
