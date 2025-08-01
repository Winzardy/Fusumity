﻿using System;
using System.Threading;
using ZenoTween;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sapientia.Collections;
using Sapientia.Utility;
using UnityEngine;

namespace UI
{
	public interface IWidget<TLayout> : IWidget
		where TLayout : UIBaseLayout
	{
		public TLayout Layout { get; }

		public void SetupLayout(TLayout layout);
		public void ClearLayout();
	}

	/// <typeparam name="TLayout">Тип верстки</typeparam>
	public abstract class UIWidget<TLayout> : UIWidget, IWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		private int _siblingIndex = -1;

		protected IWidgetAnimator<TLayout> _animator;
		protected TLayout _layout;

		private bool _visible;
		private bool _open;

		protected virtual bool UseSetAsLastSibling => false;

		/// <summary>
		///Когда виджет вообще виден на экране (начало анимации - конец анимации)
		/// </summary>
		public override bool Visible => _visible;

		/// <summary>
		/// Когда виджет открыл анимацию и уже полностью открыт
		/// </summary>
		public override bool Open => _open;

		public IWidgetAnimator<TLayout> Animator => _animator;

		public event Action<bool> VisibleChanged;

		protected internal sealed override RectTransform Root => _layout ? _layout.rectTransform : null;

		public virtual void SetupLayout(TLayout layout)
		{
			if (layout == null)
			{
				GUIDebug.LogError($"Layout can't be null ({typeof(TLayout)})", Root);
				return;
			}

			ClearLayout();

			_layout = layout;
			SetVisibleInternal(Active, false);

			OnLayoutInstalledInternal();

			TrySetDefaultAnimator(layout);
			TrySetupLayoutToAnimator();
		}

		public virtual void ClearLayout() => LayoutClearingInternal();

		protected internal sealed override void SetActiveInternal(bool active, bool immediate)
		{
			if (!ValidateLayout(out var msg))
			{
				//Может отрабатывать при Quit приложения...
				GUIDebug.LogWarning(msg);
				return;
			}

			if (active)
				OnActivatedInternal(immediate);
			else
				OnDeactivatedInternal(immediate);
		}

		private protected override void OnDisposeInternal()
		{
			LayoutClearingInternal();

			_animator?.Dispose();
			_animator = null;

			OnDispose();
			base.OnDisposeInternal();
		}

		public void SetSiblingIndex(int siblingIndex)
		{
			_siblingIndex = siblingIndex;

			if (Visible && _layout)
				_layout.rectTransform.SetSiblingIndex(_siblingIndex);
		}

		/// <summary>
		/// Подождать пока виджет станет видимым
		/// </summary>
		public async UniTask WaitUntilIsVisible(CancellationToken? cancellationToken = null)
		{
			if (cancellationToken.HasValue)
			{
				try
				{
					using var linked = DisposeCancellationTokenSource.Link(cancellationToken.Value);
					await UniTask.WaitUntil(IsVisible, cancellationToken: linked.Token);
				}
				catch (OperationCanceledException e)
				{
					_animator?.Stop(WidgetAnimationType.OPENING);
				}
			}
			else
			{
				await UniTask.WaitUntil(IsVisible, cancellationToken: DisposeCancellationToken);
			}
		}

		/// <summary>
		/// Подождать пока виджет скроется
		/// </summary>
		public async UniTask WaitUntilIsNotVisible(CancellationToken? cancellationToken = null)
		{
			if (cancellationToken.HasValue)
			{
				try
				{
					using var linkedCts = DisposeCancellationTokenSource.Link(cancellationToken.Value);
					await UniTask.WaitWhile(IsVisible, cancellationToken: linkedCts.Token);
				}
				catch (OperationCanceledException e)
				{
					_animator?.Stop(WidgetAnimationType.CLOSING);
				}
			}
			else
			{
				await UniTask.WaitWhile(IsVisible, cancellationToken: DisposeCancellationToken);
			}
		}

		/// <summary>
		/// C проверкой на наличие верстки
		/// </summary>
		public void TryForceRebuildLayout(int delayMs = 10, Action callback = null)
		{
			if (!_layout)
				return;

			ForceRebuildLayout(delayMs, callback);
		}

		public void ForceRebuildLayout(int delayMs = 10, Action callback = null)
			=> _layout.rectTransform.ForceRebuild(IsVisible, delayMs, callback);

		#region Internal

		/// <summary>
		/// Внутренний метод, имеет смысл переопределять только в случае нового поведения при активации
		/// Используйте OnShow если нужна логика при активации виджета
		/// <br/>
		/// Постфикс Internal означает что метод участвует во внутренней логике,
		/// при переопределение таких методов нужно быть аккуратным.<br/>
		/// Вызывать такие методы у наследника тоже опасно!)
		/// </summary>
		protected virtual void OnActivatedInternal(bool immediate)
		{
			if (_layout == null)
				return;

			if (_siblingIndex >= 0)
				_layout.rectTransform.SetSiblingIndex(_siblingIndex);

			if (UseSetAsLastSibling)
				_layout.rectTransform.SetAsLastSibling();

			if (_animator == null)
			{
				OnBeganOpeningInternal();
				SetVisibleInternal(true);
			}
			else
			{
				var args = GetVisibleAnimationArgs(true);
				_animator.Play(args, immediate);
			}

			SetActiveInHierarchyForChildren(true);
			OnShow();

			if (_animator == null)
				OnEndedOpeningInternal();
		}

		/// <summary>
		/// Внутренний метод, имеет смысл переопределять только в случае нового поведения при деактивации
		/// Используйте OnHide если нужна логика при деактивации виджета
		/// <br/>
		/// Постфикс Internal означает что метод участвует во внутренней логике,
		/// при переопределение таких методов нужно быть аккуратным.<br/>
		/// Вызывать такие методы у наследника тоже опасно!)
		/// </summary>
		protected virtual void OnDeactivatedInternal(bool immediate)
		{
			if (_layout == null)
				return;

			OnHide();

			if (_animator == null)
			{
				OnBeganClosingInternal();
				SetVisibleInternal(false);
			}
			else
			{
				var args = GetVisibleAnimationArgs(false);
				_animator.Play(args, immediate);
			}

			if (_animator == null)
				OnEndedClosingInternal();
		}

		protected virtual WidgetAnimationArgs GetVisibleAnimationArgs(bool visible, bool useCallback = true)
		{
			TweenCallback startCallback = null;
			TweenCallback endCallback = null;

			if (useCallback)
			{
				if (visible)
				{
					startCallback = OnBeganOpeningInternal;
					endCallback = OnEndedOpeningInternal;
				}
				else
				{
					startCallback = OnBeganClosingInternal;
					endCallback = OnEndedClosingInternal;
				}
			}

			return new WidgetAnimationArgs
			{
				key = visible ? WidgetAnimationType.OPENING : WidgetAnimationType.CLOSING,
				startCallback = startCallback,
				endCallback = endCallback
			};
		}

		/// <summary>
		/// (аниматор есть)
		/// Метод вызывается в начале проигрывания анимации (active: true)
		/// или в конце (active: false) сигнализируя что "видимость" поменялась
		///
		/// (аниматоре нет)
		/// Вкл/выкл верстку
		///
		/// v.evdokimov:
		/// Название возможно изменится)
		/// Постфикс Internal означает что метод участвует во внутренней логике,
		/// при переопределение таких методов нужно быть аккуратным.<br/>
		/// Вызывать такие методы у наследника тоже опасно!)
		/// </summary>
		void IWidget.SetVisible(bool value) => SetVisibleInternal(value);

		protected internal void SetVisibleInternal(bool visible) => SetVisibleInternal(visible, true);

		protected void SetVisibleInternal(bool visible, bool layoutClearing)
		{
			_visible = visible;
			VisibleChanged?.Invoke(visible);

			if (!_layout)
				return;

			if (!Visible && layoutClearing)
			{
				//Автоматизация по очисте верстк
				if (AutomaticLayoutClearingInternal())
					return;
			}

			OnUpdateVisibleInternal(Visible);
		}

		/// <summary>
		/// Постфикс Internal означает что метод участвует во внутренней логике,
		/// при переопределение таких методов нужно быть аккуратным.<br/>
		/// Вызывать такие методы у наследника тоже опасно!)
		/// </summary>
		/// <returns>Удалена ли верстка? например вернет false если запустили удаление с задержкой, чтобы
		/// после анимации скрывать виджет</returns>
		protected virtual bool AutomaticLayoutClearingInternal()
		{
			return false;
		}

		protected virtual void OnUpdateVisibleInternal(bool value)
		{
#if UNITY_EDITOR
			if (_layout.gameObject != _layout.rectTransform.gameObject)
			{
				GUIDebug.LogError($"RestTransform is set incorrectly in layout [ {_layout.name} ]", _layout);
			}
#endif
			_layout.gameObject.SetActive(value);
		}

		/// <summary>
		/// Установить новый аниматор, через создание.
		/// Лучше использовать <see cref="SetAnimator"/>.
		/// </summary>
		/// <typeparam name="T">Тип аниматора</typeparam>
		protected T SetAnimator<T>()
			where T : IWidgetAnimator<TLayout>, new()
		{
			var animator = new T();
			SetAnimator(animator);
			return animator;
		}

		/// <summary>
		/// Установить новый аниматор.
		/// </summary>
		/// <typeparam name="T">Тип аниматора</typeparam>
		protected void SetAnimator<T>(T animator)
			where T : IWidgetAnimator<TLayout>
		{
			_animator?.Dispose();

			_animator = animator;
			_animator?.Setup(this);

			TrySetupLayoutToAnimator();
		}

		private void TrySetupLayoutToAnimator()
		{
			if (_animator == null)
				return;

			if (_layout == null)
				return;

			if (_animator.SetupLayout(_layout))
			{
				var args = GetVisibleAnimationArgs(Active, false);
				_animator.Play(args, true);
			}
		}

		/// <summary>
		/// Постфикс Internal означает что метод участвует во внутренней логике,
		/// при переопределение таких методов нужно быть аккуратным.<br/>
		/// Вызывать такие методы у наследника тоже опасно!)
		/// </summary>
		protected virtual void LayoutClearingInternal()
		{
			if (_layout == null)
				return;

			OnLayoutClearedInternal();
			DisposeAndClearChildren();

			_layout = null;
		}

		public override void Initialize() => OnInitialized();

		protected virtual bool ValidateLayout(out string msg)
		{
			msg = $"Need to assign layout before change state (active) [ {GetType()} ] ({nameof(SetupLayout)} method)";
			return _layout;
		}

		protected internal virtual void OnBeganOpeningInternal() => OnBeganOpening();

		protected internal virtual void OnEndedOpeningInternal()
		{
			_open = true;
			OnEndedOpening();
		}

		protected internal virtual void OnBeganClosingInternal()
		{
			_open = false;
			OnBeganClosing();
		}

		protected internal virtual void OnEndedClosingInternal()
		{
			OnEndedClosing();
			SetActiveInHierarchyForChildren(false);
		}

		protected internal virtual void OnLayoutInstalledInternal() => OnLayoutInstalled();

		protected internal virtual void OnLayoutClearedInternal() => OnLayoutCleared();

		#endregion

		/// <summary>
		/// Вызывается при установке верстки (<see cref="UIBaseLayout"/>)
		/// </summary>
		protected virtual void OnLayoutInstalled()
		{
		}

		/// <summary>
		/// Вызывается при удалении верстки (<see cref="UIBaseLayout"/>)
		/// Например при <see cref="Dispose"/> виджета или переназначении верскти
		/// Верстка еще доступна для очистки при вызове
		/// </summary>
		protected virtual void OnLayoutCleared()
		{
		}

		/// <summary>
		/// Аналог конструктора
		/// </summary>
		protected virtual void OnInitialized()
		{
		}

		/// <summary>
		/// Аналог Dispose
		/// </summary>
		protected virtual void OnDispose()
		{
		}

		/// <summary>
		/// Началась анимация открытия (гарантия вызова, даже если анимации нет)
		/// </summary>
		protected virtual void OnBeganOpening()
		{
		}

		/// <summary>
		/// Закончилась анимация открытия (гарантия вызова, даже если анимации нет)
		/// </summary>
		protected virtual void OnEndedOpening()
		{
		}

		/// <summary>
		/// Началась анимация закрытия (гарантия вызова, даже если анимации нет)
		/// </summary>
		protected virtual void OnBeganClosing()
		{
		}

		/// <summary>
		/// Закончилась анимация закрытия (гарантия вызова, даже если анимации нет)
		/// </summary>
		protected virtual void OnEndedClosing()
		{
		}

		TLayout IWidget<TLayout>.Layout => _layout;

		private void TrySetDefaultAnimator(TLayout layout)
		{
			if (_animator != null)
				return;

			if (!layout.UseLayoutAnimations)
				return;

			var visibleAnimationEmpty =
				layout.openingSequence.IsNullOrEmpty() && layout.closingSequence.IsNullOrEmpty();
			var customAnimationEmpty = layout.customSequences.IsNullOrEmpty();

			if (visibleAnimationEmpty && customAnimationEmpty)
				return;

			SetAnimator<DefaultWidgetAnimator>();
		}
	}
}
