using System;
using Sapientia.Extensions;
using UI;

namespace Fusumity.MVVM.UI
{
	/// <summary>
	/// Вью с анимациями видимости из верстки (<see cref="AnimationType.OPENING"/> / <see cref="AnimationType.CLOSING"/>).
	/// Открывается при бинде модели, закрывается при null или смене модели;
	/// перебинд нового контента происходит по завершению closing-анимации.
	/// Наследник реализует <see cref="OnApply"/> / <see cref="OnRelease"/> вместо OnUpdate / OnClear.
	/// </summary>
	public abstract class UIAnimatedView<TViewModel, TLayout> : UIView<TViewModel, TLayout>
		where TViewModel : class
		where TLayout : UIBaseLayout
	{
		private readonly UIAnimator<TLayout> _animator;

		/// <summary>Идет closing-анимация (объект еще активен)</summary>
		public bool IsClosing { get => IsActive && _animator.LastKey == AnimationType.CLOSING; }

		/// <summary>Closing-анимация доиграла, объект скрыт</summary>
		public event Action Closed;

		protected UIAnimatedView(TLayout layout) : base(layout)
		{
			AddDisposable(_animator = new UIAnimator<TLayout>(layout));
		}

		protected sealed override void OnUpdate(TViewModel viewModel)
		{
			// Идет закрытие под предыдущую модель — перебиндимся в HandleAnimationEnded
			if (IsActive && _animator.LastKey == AnimationType.CLOSING)
				return;

			OnApply(viewModel);
			Play(AnimationType.OPENING);
		}

		protected sealed override void OnClear(TViewModel viewModel)
		{
			OnRelease(viewModel);

			Play(AnimationType.CLOSING);
		}

		protected sealed override void OnNullViewModel() => Play(AnimationType.CLOSING);

		/// <summary>Бинд модели в верстку (вместо OnUpdate)</summary>
		protected abstract void OnApply(TViewModel viewModel);

		/// <summary>Отписка от модели (вместо OnClear)</summary>
		protected virtual void OnRelease(TViewModel viewModel)
		{
		}

		private void Play(string key)
		{
			if (_animator.LastKey == key)
				return;

			// Первый Play — мгновенный и без колбека: первичный бинд не анимируем
			if (_animator.LastKey.IsNullOrEmpty())
			{
				_animator.Play(key, immediate: true);
				return;
			}

			_animator.Play(new WidgetAnimationArgs
			{
				key = key,
				endCallback = HandleAnimationEnded,
			});
		}

		private void HandleAnimationEnded(string key)
		{
			if (key != AnimationType.CLOSING)
				return;

			Closed?.Invoke();

			// Модель успела смениться, пока играло закрытие — открываемся заново с актуальной
			if (ViewModel == null)
				return;

			OnApply(ViewModel);
			Play(AnimationType.OPENING);
		}
	}
}
