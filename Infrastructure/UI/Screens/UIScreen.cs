using System;
using System.Collections.Generic;
using AssetManagement;
using Sapientia;
using Sapientia.Extensions;
using UI.Layers;

namespace UI.Screens
{
	public interface IScreen : IWidget, IIdentifiable
	{
		public void RequestClose();
		public Type GetArgsType();

		internal event Action<IScreen> RequestedClose;

		internal void Initialize(UIScreenConfig config);

		/// <param name="args">Важно подметить, что аргументы могут быть изменены в процессе использования окна!
		///При запросе открытия окна аргументы копируются в окно, далее могут измениться и эти аргументы система может
		///получить через GetArgs() в своих целях (скрыть окно на время).
		///Получается аргументы задают состояние окна от начала до конца использования.</param>
		internal void Show(object args);

		//TODO: поймал кейс в котором окно для корника осталось в очереди, потому что его никто не закрыл (PauseScreen)
		internal bool CanShow(object args, out string error);

		internal void Hide(bool reset, bool immediate = false);
		internal object GetArgs();

		internal IDisposable Prepare(Action callback);
	}

	public abstract class UIScreen<TLayout> : UIBaseScreen<TLayout, EmptyArgs>
		where TLayout : UIBaseScreenLayout
	{
		private protected sealed override object GetArgs() => null;

		protected sealed override bool CanShow(ref EmptyArgs _, out string error) => CanShow(out error);

		protected virtual bool CanShow(out string error)
		{
			error = string.Empty;
			return true;
		}
	}

	public abstract class UIScreen<TLayout, TArgs> : UIBaseScreen<TLayout, TArgs>
		where TLayout : UIBaseScreenLayout
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

	/// <summary>
	/// Название типа Screen должно соотвествовать его конфигу (Entry)!
	/// </summary>
	public abstract class UIBaseScreen<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IScreen
		where TLayout : UIBaseScreenLayout
	{
		private const string LAYOUT_PREFIX_NAME = "[Screen] ";

		protected TArgs _args;

		private UIScreenConfig _config;

		private bool? _resetting;

		string IIdentifiable.Id => Id;

		public ref TArgs vm => ref _args;

		#region Layout

		protected override ComponentReferenceEntry LayoutReference => _config.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _config.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _config.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _config.layout.preloadAssets;

		#endregion

		protected override string Layer => LayerType.SCREENS;

		protected override bool UseSetAsLastSibling => false;

		private event Action<IScreen> RequestedClose;

		event Action<IScreen> IScreen.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		public sealed override void SetupLayout(TLayout layout)
		{
			if (_animator == null)
				SetAnimator<DefaultScreenAnimator>();

			base.SetupLayout(layout);
		}

		[Obsolete(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE, true)]
		public sealed override void Initialize() =>
			throw new Exception(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT.Format(GetType().Name));

		IDisposable IScreen.Prepare(Action callback) => Prepare(callback);

		void IScreen.Initialize(UIScreenConfig config)
		{
			_config = config;

			base.Initialize();
		}

		void IScreen.Show(object boxedArgs)
		{
			if (boxedArgs != null)
			{
				var args = UnboxedArgs(boxedArgs);

				if (Active)
				{
					if (_args.Equals(args))
						return;

					EnableSuppress();

					//Неявное поведение...
					//Нужно вызывать OnHide у окна если хотим
					//переоткрыть окно с новыми аргументами
					SetActive(false, true, false);
				}

				_args = args;
			}

			var suppressAnyFlag = suppressFlag != SuppressFlag.None;
			SetActive(true, suppressAnyFlag);
			DisableSuppress();
		}

		object IScreen.GetArgs() => GetArgs();
		private protected virtual object GetArgs() => _args;

		bool IScreen.CanShow(object boxedArgs, out string error)
		{
			var args = UnboxedArgs(boxedArgs);
			return CanShow(ref args, out error);
		}

		void IScreen.Hide(bool reset, bool immediate = false)
		{
			_resetting = reset;
			SetActive(false, immediate);
		}

		protected override bool AutomaticLayoutClearingInternal()
		{
			if (_resetting.HasValue(out var resetting) && !resetting)
				return false;

			return base.AutomaticLayoutClearingInternal();
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

		public Type GetArgsType() => typeof(TArgs);

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			if (_animator == null)
				SetAnimator<DefaultScreenAnimator>();
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

		protected virtual bool CanShow(ref TArgs args, out string error)
		{
			error = string.Empty;
			return true;
		}

		private TArgs UnboxedArgs(object boxedArgs)
		{
			if (boxedArgs == null)
				throw new ArgumentException($"Passed null args ({typeof(TArgs)}) to screen of type [{GetType()}]");

			if (boxedArgs is not TArgs args)
				throw new Exception(
					$"Passed wrong args ({boxedArgs.GetType()}) to screen of type [{GetType()}] (need type: {typeof(TArgs)})");

			return args;
		}
	}
}
