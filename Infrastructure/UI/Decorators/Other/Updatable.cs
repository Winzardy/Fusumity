using Fusumity.Reactive;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public abstract class Updatable : MonoBehaviour
	{
		[System.Flags]
		protected internal enum UpdateMode
		{
			None,
			Update = 1 << 0,
			LateUpdate = 1 << 1,
			All = Update | LateUpdate
		}

		private bool _subscribed;

		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		protected virtual UpdateMode Mode { get => UpdateMode.Update; }

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_subscribed = true;

			if (Mode.HasFlags(UpdateMode.Update))
				UnityLifecycle.UpdateEvent.Subscribe(OnUpdate);
			if (Mode.HasFlags(UpdateMode.LateUpdate))
				UnityLifecycle.LateUpdateEvent.Subscribe(OnLateUpdate);

			OnEnabled();
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_subscribed = false;

			if (Mode.HasFlags(UpdateMode.Update))
				UnityLifecycle.UpdateEvent.UnSubscribe(OnUpdate);
			if (Mode.HasFlags(UpdateMode.LateUpdate))
				UnityLifecycle.LateUpdateEvent.UnSubscribe(OnLateUpdate);

			OnDisabled();
		}

		protected virtual void OnUpdate()
		{
		}

		protected virtual void OnLateUpdate()
		{
		}

		protected virtual void OnEnabled()
		{
		}

		protected virtual void OnDisabled()
		{
		}

		[Space]
		[PropertyOrder(10)]
		[SerializeField]
		private bool _updateInEditMode = true;

#if UNITY_EDITOR
		private void Update()
		{
			if (Mode.HasFlags(UpdateMode.Update))
				return;
			if (_updateInEditMode && !Application.isPlaying)
				OnUpdate();
		}

		private void LateUpdate()
		{
			if (Mode.HasFlags(UpdateMode.LateUpdate))
				return;
			if (_updateInEditMode && !Application.isPlaying)
				OnLateUpdate();
		}
#endif
	}
}
