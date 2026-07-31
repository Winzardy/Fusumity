using UnityEngine;

namespace UI
{
	public abstract class ActivationBehaviour : MonoBehaviour
	{
		private bool _subscribed;

		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_subscribed = true;

			OnEnabledInternal();
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_subscribed = false;

			OnDisabledInternal();
		}

		protected virtual void OnEnabledInternal() => OnEnabled();
		protected virtual void OnDisabledInternal() => OnDisabled();

		protected virtual void OnEnabled()
		{
		}

		protected virtual void OnDisabled()
		{
		}
	}
}
