using System.Threading;
using Cysharp.Threading.Tasks;

namespace UI
{
	public abstract class UIWidget<TLayout, TArgs> : UIWidget<TLayout>
		where TLayout : UIBaseLayout
		//where TArgs : struct
	{
		protected TArgs _args;

		private bool _clearedArgs = true;

		public ref readonly TArgs args => ref _args;

		public override void SetupLayout(TLayout layout)
		{
			OnSetupDefaultAnimator();

			base.SetupLayout(layout);
		}

		/// <summary>
		/// Обновляем аргументы и активируем виджет, если он не был активирован
		/// </summary>
		/// <param name="equals">Проверяет новые аргументы с предыдущими, если они равны, то не обновляет</param>
		public void Show(in TArgs args, bool immediate = false, bool equals = true)
		{
			_immediate = immediate;

			Update(in args, equals);

			if (Active)
				return;

			SetActive(true, immediate, false);
		}

		/// <summary>
		/// Аналог SetActive(false), но дополнительно сбрасывает аргументы
		/// </summary>
		public void Hide(bool reset = true, bool immediate = false)
		{
			_immediate = immediate;

			if (!Active)
				return;

			SetActive(false, immediate, false);

			if (reset)
				Reset();
		}

		/// <summary>
		/// Обновляет аргументы и если виджет активирован, то
		/// сначала вызовет SetActive(false) форсом и потом форсом SetActive(true)
		/// Могут быть проблемы с отработкой анимации
		/// Пока подумаю как иначе обновлять аргументы, чтобы можно было делать это во время анимации...
		/// </summary>
		/// <param name="equals">Проверяет новые аргументы с предыдущими, если они равны, то не обновляет</param>
		public void Update(in TArgs args, bool equals = true)
		{
			//Зачем обновлять если там одно и тоже
			if (equals && _args.Equals(args))
				return;

			var cacheActive = Active;

			if (cacheActive)
				SetActive(false, true, false);

			_args = args;

			if (cacheActive)
				SetActive(true, true, false);
		}

		/// <summary>
		/// Лучше использовать Show(in TArgs args) и потом WaitUntilIsVisible(), но это минор
		/// </summary>
		public async UniTask ShowAsync(TArgs args, bool equals = true, CancellationToken? cancellationToken = null)
		{
			Show(in args, equals: equals);
			await WaitUntilIsVisible(cancellationToken);
		}

		public async UniTask HideAsync(bool reset = true, CancellationToken? cancellationToken = null)
		{
			Hide(false);
			await WaitUntilIsNotVisible(cancellationToken);
			if (reset)
				Reset(false);
		}

		protected sealed override void OnShow()
		{
			_clearedArgs = false;
			OnShow(ref _args);
		}

		protected abstract void OnShow(ref TArgs args);

		protected sealed override void OnHide() => OnHide(ref _args);

		/// <summary>
		/// OnHide с аргументом, чтобы произвести отписку на данные из аргументов (по надобности),
		/// чтобы не пришлось их кешировать
		/// </summary>
		protected virtual void OnHide(ref TArgs args)
		{
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

		protected virtual void OnSetupDefaultAnimator()
		{
		}
	}
}
