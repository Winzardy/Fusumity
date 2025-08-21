using System;
using System.ComponentModel;
using DG.Tweening;
using Sapientia.Collections;
using UnityEngine;
using ZenoTween;

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

		/// <summary>
		/// Флаг для подавления повторного запуска анимации и вызова событий <see cref="Shown"/> и <see cref="Hidden"/>.
		/// </summary>
		private SuppressFlag _suppressFlag;

		/// <inheritdoc cref="_suppressFlag"/>
		protected internal SuppressFlag suppressFlag => _suppressFlag;

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

		public sealed override event WidgetShownDelegate Shown;
		public sealed override event WidgetHiddenDelegate Hidden;
		public sealed override event WidgetLayoutInstalledDelegate LayoutInstalled;
		public sealed override event WidgetLayoutClearedDelegate LayoutCleared;

		public sealed override RectTransform RectTransform => _layout ? _layout.rectTransform : null;

		public virtual void SetupLayout(TLayout layout)
		{
			if (layout == null)
			{
				GUIDebug.LogError($"Layout can't be null ({typeof(TLayout)})", RectTransform);
				return;
			}

			ClearLayout();

			_layout = layout;
			SetVisibleInternal(Active, false);

			OnLayoutInstalledInternal();

			TrySetDefaultAnimator(layout);
			TrySetupLayoutToAnimator();

			LayoutInstalled?.Invoke(_layout);
		}

		public virtual void ClearLayout() => LayoutClearingInternal();

		protected internal sealed override void SetActiveInternal(bool active, bool immediate)
		{
			if (!ValidateLayout(out var msg))
			{
				// Может отрабатывать при OnApplicationQuit...
				GUIDebug.LogWarning(msg);
				return;
			}

			if (active)
				OnActivatedInternal(immediate);
			else
				OnDeactivatedInternal(immediate);
		}

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		private protected override void OnDisposeInternal()
		{
			LayoutClearingInternal();

			DisposeAndSetNullSafe(ref _animator);

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
		/// C проверкой на наличие верстки
		/// </summary>
		public void ForceRebuildLayoutSafe(int delayMs = 10, Action callback = null)
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
		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected virtual void OnActivatedInternal(bool immediate)
		{
			if (_layout == null)
				return;

			if (_siblingIndex >= 0)
				_layout.rectTransform.SetSiblingIndex(_siblingIndex);

			if (UseSetAsLastSibling)
				_layout.rectTransform.SetAsLastSibling();

			var withoutAnimation = _animator == null ||
				_suppressFlag.HasFlag(SuppressFlag.Animation);

			OnPrepareOpening();

			if (withoutAnimation)
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

			if (withoutAnimation)
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
		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected virtual void OnDeactivatedInternal(bool immediate)
		{
			if (_layout == null)
				return;

			OnHide();

			var withoutAnimation = _animator == null
				|| _suppressFlag.HasFlag(SuppressFlag.Animation);
			if (withoutAnimation)
			{
				OnBeganClosingInternal();
				SetVisibleInternal(false);
			}
			else
			{
				var args = GetVisibleAnimationArgs(false);
				_animator.Play(args, immediate);
			}

			if (withoutAnimation)
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

		protected sealed override void OnVisibleOperationCanceledException(bool openingOrClosing)
			=> _animator?.Stop(openingOrClosing ? WidgetAnimationType.OPENING : WidgetAnimationType.CLOSING);

		protected internal void SetVisibleInternal(bool visible) => SetVisibleInternal(visible, true);

		protected void SetVisibleInternal(bool visible, bool layoutClearing)
		{
			var changed = _visible != visible;

			_visible = visible;

			if (!_suppressFlag.HasFlag(SuppressFlag.Events) && changed)
			{
				if (visible)
					Shown?.Invoke(this);
				else
					Hidden?.Invoke(this);
			}

			if (!_layout)
				return;

			if (!_suppressFlag.HasFlag(SuppressFlag.Events))
			{
				if (!Visible && layoutClearing)
				{
					// Автоматизация по очистке верстки
					if (AutomaticLayoutClearingInternal())
						return;
				}
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

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnBeganOpeningInternal()
		{
			OnBeganOpening();
		}

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnEndedOpeningInternal()
		{
			_open = true;
			OnEndedOpening();
		}

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnBeganClosingInternal()
		{
			_open = false;
			OnBeganClosing();
		}

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnEndedClosingInternal()
		{
			OnEndedClosing();
			SetActiveInHierarchyForChildren(false);
		}

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnLayoutInstalledInternal() => OnLayoutInstalled();

		/// <remarks>
		/// Базовые методы формата On{Name}Internal (префикс On и постфикс Internal)
		/// обязательно нужно вызывать если переопределяем!
		/// </remarks>
		protected internal virtual void OnLayoutClearedInternal()
		{
			LayoutCleared?.Invoke(_layout);
			OnLayoutCleared();
		}

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

		/// <summary>
		/// Происходит перед вызовом аниматора или просто OnShow!
		/// </summary>
		protected virtual void OnPrepareOpening()
		{
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected internal void EnableSuppress(SuppressFlag flag = SuppressFlag.All) => _suppressFlag = flag;

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected internal void DisableSuppress() => _suppressFlag = SuppressFlag.None;

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Flags]
		public enum SuppressFlag
		{
			None,

			Events = 1 << 0,
			Animation = 1 << 1,

			All = Events | Animation
		}
	}
}
