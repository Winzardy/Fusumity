using System;
using System.Collections.Generic;
using AssetManagement;
using Sapientia;
using Sapientia.Extensions;
using UI.Layers;

namespace UI.Windows
{
	public interface IWindow : IWidget, IIdentifiable
	{
		public void RequestClose();

		internal event Action<IWindow> RequestedClose;

		internal void Initialize(UIWindowConfig config);

		/// <param name="args">Важно подметить, что аргументы могут быть изменены в процессе использования окна!
		///При запросе открытия окна аргументы копируются в окно, далее могут измениться и эти аргументы система может
		///получить через GetArgs() в своих целях (скрыть окно на время).
		///Получается аргументы задают состояние окна от начала до конца использования.</param>
		internal void Show(IWindowArgs args);

		//TODO: поймал кейс в котором окно для корника осталось в очереди, потому что его никто не закрыл (PauseWindow)
		internal bool CanShow(IWindowArgs args, out string error);

		internal void Hide(bool reset);
		internal IWindowArgs GetArgs();
	}

	public abstract class UIWindow<TLayout> : UIBaseWindow<TLayout, EmptyWindowArgs>
		where TLayout : UIBaseWindowLayout
	{
		private protected sealed override IWindowArgs GetArgs() => null;

		protected sealed override bool CanShow(ref EmptyWindowArgs _, out string error) => CanShow(out error);

		protected virtual bool CanShow(out string error)
		{
			error = string.Empty;
			return true;
		}
	}

	public abstract class UIWindow<TLayout, TArgs> : UIBaseWindow<TLayout, TArgs>
		where TLayout : UIBaseWindowLayout
		where TArgs : IWindowArgs
	{
		protected sealed override void OnShow() => OnShow(ref _args);

		protected abstract void OnShow(ref TArgs args);

		protected sealed override void OnHide() => OnHide(ref _args);

		protected virtual void OnHide(ref TArgs args)
		{
		}
	}

	public abstract class UIBaseWindow<TLayout, TArgs> : UIClosableRootWidget<TLayout>, IWindow
		where TLayout : UIBaseWindowLayout
		where TArgs : IWindowArgs
	{
		private const string LAYOUT_PREFIX_NAME = "[Window] ";

		private bool? _resetting;

		protected UIWindowConfig _config;

		protected TArgs _args;

		string IIdentifiable.Id => Id;

		private event Action<IWindow> RequestedClose;

		event Action<IWindow> IWindow.RequestedClose { add => RequestedClose += value; remove => RequestedClose -= value; }

		protected override ComponentReferenceEntry LayoutReference => _config.layout.LayoutReference;
		protected override bool LayoutAutoDestroy => _config.layout.HasFlag(LayoutAutomationMode.AutoDestroy);
		protected override int LayoutAutoDestroyDelayMs => _config.layout.autoDestroyDelayMs;
		protected override List<AssetReferenceEntry> PreloadAssets => _config.layout.preloadAssets;

		protected override string Layer => LayerType.WINDOWS;

		public sealed override void SetupLayout(TLayout layout)
		{
			OnSetupDefaultAnimator();

			base.SetupLayout(layout);
		}

		[Obsolete(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE, true)]
		public sealed override void Initialize()
			=> throw new Exception(INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT.Format(GetType().Name));

		void IWindow.Initialize(UIWindowConfig config)
		{
			_config = config;

			base.Initialize();
		}

		void IWindow.Show(IWindowArgs boxedArgs)
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

		IWindowArgs IWindow.GetArgs() => GetArgs();
		private protected virtual IWindowArgs GetArgs() => _args;

		bool IWindow.CanShow(IWindowArgs boxedArgs, out string error)
		{
			var args = UnboxedArgs(boxedArgs);
			return CanShow(ref args, out error);
		}

		void IWindow.Hide(bool reset)
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

		protected override void OnUpdateVisibleInternal(bool value)
		{
			//TODO: очень многое завязано на SetActive...
// 			if (_layout.canvas)
// 			{
// 				_layout.canvas.enabled = value;
//
// #if UNITY_EDITOR
// 				const string DISABLE_STATUS = " (disabled)";
//
// 				_layout.name = value ? _layout.name.Replace(DISABLE_STATUS, string.Empty) :
// 					_layout.name.Contains(DISABLE_STATUS) ? _layout.name :
// 					_layout.name + DISABLE_STATUS;
// #endif
// 				return;
// 			}

			base.OnUpdateVisibleInternal(value);
		}

		public override void RequestClose() => RequestedClose?.Invoke(this);

		protected override string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		protected virtual void OnSetupDefaultAnimator()
		{
			if (_animator == null)
				SetAnimator<DefaultWindowAnimator>();
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

		private TArgs UnboxedArgs(IWindowArgs boxedArgs)
		{
			if (boxedArgs == null)
				throw new ArgumentException($"Passed null args ({typeof(TArgs)}) to window of type [{GetType()}]");

			if (boxedArgs is not TArgs args)
				throw new Exception(
					$"Passed wrong args ({boxedArgs.GetType()}) to window of type [{GetType()}] (need type: {typeof(TArgs)})");

			return args;
		}
	}

	public class EmptyWindowArgs : IWindowArgs
	{
	}

	public interface IWindowArgs
	{
	}
}
