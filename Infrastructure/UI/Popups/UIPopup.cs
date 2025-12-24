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
		public PopupMode Mode { get; }
		public void RequestClose();

		internal event Action<IPopup> RequestedClose;

		internal void Initialize(in UIPopupConfig config);

		/// <param name="args">Важно подметить, что аргументы могут быть изменены в процессе использования попапа!
		/// При запросе открытия окна аргументы копируются в окно, далее могут измениться и эти аргументы система может
		/// получить через GetArgs() в своих целях (скрыть попап на время).
		/// Получается аргументы задают состояние окна от начала до конца использования.</param>
		internal void Show(object args);

		//TODO: поймал кейс в котором окно для корника осталось в очереди, потому что его никто не закрыл (PauseWindow)
		internal bool CanShow(object args, out string error);

		internal void Hide(bool reset, bool immediate = false);
		internal object GetArgs();
	}

	public abstract class UIPopup<TLayout> : UIBasePopup<TLayout, EmptyArgs>
		where TLayout : UIBasePopupLayout
	{
		private protected sealed override object GetArgs() => null;

		protected sealed override bool CanShow(ref EmptyArgs _, out string error) => CanShow(out error);

		protected virtual bool CanShow(out string error)
		{
			error = string.Empty;
			return true;
		}
	}

	public abstract class UIPopup<TLayout, TArgs> : UIBasePopup<TLayout, TArgs>
		where TLayout : UIBasePopupLayout
	{
		private bool _suppressHide;
		private bool _clearedArgs;

		protected sealed override void OnShow()
		{
			_clearedArgs = false;

			if (ShouldSkipActivation(in _args, out var reset))
			{
				if (reset)
					Reset(false);
				return;
			}

			if (_args is IRequestClose closable)
				closable.RequestedClose += RequestClose;

			OnShow(ref _args);
		}

		protected abstract void OnShow(ref TArgs args);

		protected sealed override void OnHide()
		{
			if (ShouldSkipActivation(in _args, out _))
				return;

			if (_args is IRequestClose closable)
				closable.RequestedClose -= RequestClose;

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

		protected override void OnReset(bool deactivate)
		{
			if (deactivate)
				SetActive(false, true);
			else if (Active && !_clearedArgs)
				OnHide(); //Отписка со старым args!

			_args = default;
			_clearedArgs = true;

			base.OnReset(deactivate);
		}

		protected virtual bool ShouldSkipActivation(in TArgs args, out bool reset)
		{
			reset = true;
			return args == null;
		}

		protected override void OnAfterSetupTemplate() => _suppressHide = false;
	}

	public abstract class UIBasePopup<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IPopup
		where TLayout : UIBasePopupLayout
	{
		private const string LAYOUT_PREFIX_NAME = "[Popup] ";

		private UIPopupConfig _config;

		private bool? _resetting;

		protected TArgs _args;
		private object _context;

		string IIdentifiable.Id => Id;

		private event Action<IPopup> RequestedClose;

		event Action<IPopup> IPopup.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		protected override string Layer => LayerType.POPUPS;

		protected virtual PopupMode Mode => PopupMode.Default;

		PopupMode IPopup.Mode => Mode;

		protected ref TArgs vm => ref _args;

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

		void IPopup.Initialize(in UIPopupConfig config)
		{
			_config = config;

			base.Initialize();
		}

		void IPopup.Show(object boxedArgs)
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

		bool IPopup.CanShow(object boxedArgs, out string error)
		{
			var args = UnboxedArgs(boxedArgs);
			return CanShow(ref args, out error);
		}

		void IPopup.Hide(bool reset, bool immediate = false)
		{
			_resetting = reset;
			SetActive(false, immediate);
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

		object IPopup.GetArgs() => GetArgs();

		private protected virtual object GetArgs() => _args;

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

		private TArgs UnboxedArgs(object boxedArgs)
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
}
