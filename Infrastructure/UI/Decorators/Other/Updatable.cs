using Fusumity.Reactive;
using UnityEngine;

namespace UI
{
	public interface ILateUpdatable
	{
		void OnLateUpdate();
	}

#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public class Updatable : MonoBehaviour
	{
		[SerializeField]
		private bool _updateInEditMode = true;

		private bool _subscribed;
		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		private ILateUpdatable _lateUpdatable;

		private void Awake()
		{
			if (this is ILateUpdatable lateUpdatable)
			{
				_lateUpdatable = lateUpdatable;
			}
		}

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_subscribed = true;
			UnityLifecycle.UpdateEvent.Subscribe(OnUpdate);

			if (_lateUpdatable != null)
				UnityLifecycle.UpdateEvent.Subscribe(_lateUpdatable.OnLateUpdate);

			OnEnabled();
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_subscribed = false;
			UnityLifecycle.UpdateEvent.UnSubscribe(OnUpdate);
			if (_lateUpdatable != null)
				UnityLifecycle.UpdateEvent.UnSubscribe(_lateUpdatable.OnLateUpdate);

			OnDisabled();
		}

		protected virtual void OnUpdate()
		{
		}

		protected virtual void OnEnabled()
		{
		}

		protected virtual void OnDisabled()
		{
		}

#if UNITY_EDITOR
		private void Update()
		{
			if (_updateInEditMode && !Application.isPlaying)
				OnUpdate();
		}
#endif
	}
}
