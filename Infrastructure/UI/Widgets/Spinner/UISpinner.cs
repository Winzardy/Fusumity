using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Дефолтная реализация, для более сложных кейсов есть Generic
	/// </summary>
	public class UISpinner : UISpinner<UISpinnerLayout>
	{
		public UISpinner() : base()
		{
		}

		public UISpinner(UISpinnerLayout layout) : base(layout)
		{
		}
	}

	public class UISpinner<TLayout> : UIWidget<TLayout>, ISpinner
		where TLayout : UISpinnerLayout
	{
		private HashSet<object> _requesters;
		private HashSet<object> _blockers;

		public UISpinner() : base()
		{
		}

		public UISpinner(TLayout layout) : base(layout)
		{
		}

		private protected override void OnDisposedInternal()
		{
			base.OnDisposedInternal();

			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _requesters);
			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _blockers);
		}

		public override void SetupLayout(TLayout layout)
		{
			if (_animator == null)
				SetAnimator<DefaultSpinnerAnimator>();

			base.SetupLayout(layout);
		}

		/// <inheritdoc cref="AddRequester"/>
		public bool Show(object requester, bool immediate = false, bool useCacheImmediate = true)
			=> AddRequester(requester, immediate, useCacheImmediate);

		/// <summary>
		/// Добавляет нового "запросившего", если "запросивших" не было до этого то активирует виджет
		/// </summary>
		public bool AddRequester(object requester, bool immediate = false, bool useCacheImmediate = true)
		{
			_requesters ??= HashSetPool<object>.Get();
			if (_requesters.Add(requester))
			{
				if (_blockers != null && !_blockers.IsEmpty())
					return false;

				SetActive(true, immediate, useCacheImmediate);
				return true;
			}

			return false;
		}

		public bool Show(object requester) => Show(requester, false);

		public bool Hide(object requester) => Hide(requester, false);

		/// <inheritdoc cref="RemoveRequester"/>
		public bool Hide(object requester, bool immediate = false)
			=> RemoveRequester(requester, immediate);

		/// <summary>
		/// Убирает "запросившего", если "запросивших" больше нет, то деактивирует виджет
		/// </summary>
		public bool RemoveRequester(object requester, bool immediate = false)
		{
			if (_requesters == null || _requesters.Remove(requester))
			{
				if (_requesters != null && !_requesters.IsEmpty())
					return false;

				SetActive(false, immediate);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Добавляет блокер на показ, если виджет уже показан, то скроет его
		/// </summary>
		public bool AddBlocker(object blocker, bool immediate = false)
		{
			_blockers ??= HashSetPool<object>.Get();
			if (_blockers.Add(blocker))
			{
				SetActive(false, immediate);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Этот метод вызывается если Hide не отрабатывает...
		/// </summary>
		/// <remarks>Гребанные часики...</remarks>
		public void ForceHide(bool immediate = false)
		{
			_requesters.Clear();
			SetActive(false, immediate);
			GUIDebug.LogWarning("Force hide spinner...");
		}

		/// <summary>
		/// Удаляет блокер на показ, если у виджета есть "запросившие" активирует виджет обратно
		/// </summary>
		public bool RemoveBlocker(object blocker, bool immediate = false)
		{
			if (_blockers != null && _blockers.Remove(blocker))
			{
				if (_blockers != null && !_blockers.IsEmpty())
					return true;

				if (_requesters != null && _requesters.IsEmpty())
					return true;

				SetActive(true, immediate);
				return true;
			}

			return false;
		}

		public void Pause() => _animator.Pause(WidgetAnimationType.SPINNING);

		public void Resume() => _animator.Resume(WidgetAnimationType.SPINNING);

		protected override void OnShow() => _animator.Play(WidgetAnimationType.SPINNING);

		protected internal override void OnEndedClosingInternal() => _animator.Stop(WidgetAnimationType.SPINNING, true);
	}

	public interface ISpinner
	{
		public bool Show(object requester);
		public bool Hide(object requester);
	}
}
