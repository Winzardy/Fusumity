using Fusumity.Reactive;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public abstract class ReactiveBehaviour : ActivationBehaviour
	{
		[System.Flags]
		protected internal enum UpdateMode
		{
			None,
			Update = 1 << 0,
			LateUpdate = 1 << 1,
			All = Update | LateUpdate
		}

		protected virtual UpdateMode Mode { get => UpdateMode.Update; }

		protected override void OnEnabledInternal()
		{
			if (Mode.HasFlags(UpdateMode.Update))
				UnityLifecycle.UpdateEvent.Subscribe(OnUpdate);
			if (Mode.HasFlags(UpdateMode.LateUpdate))
				UnityLifecycle.LateUpdateEvent.Subscribe(OnLateUpdate);

			OnEnabled();
		}

		protected override void OnDisabledInternal()
		{
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
