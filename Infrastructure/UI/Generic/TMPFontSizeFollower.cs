using Fusumity.Reactive;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	[ExecuteAlways, RequireComponent(typeof(TMP_Text))]
	public class TMPFontSizeFollower : MonoBehaviour
	{
		[SerializeField, ReadOnly]
		private TMP_Text _self;

		[SerializeField]
		private TMP_Text _target;

		private bool _subscribed;

		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_subscribed = true;
			UnityLifecycle.LateUpdateEvent.Subscribe(OnLateUpdate);
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_subscribed = false;
			UnityLifecycle.LateUpdateEvent.UnSubscribe(OnLateUpdate);
		}

		private void OnLateUpdate()
		{
			if (!_target)
				return;
			_self.fontSize = _target.fontSize;
		}

		private void Reset()
		{
			_self = GetComponent<TMP_Text>();
		}
#if UNITY_EDITOR
		private void Update()
		{
			if (!Application.isPlaying)
				OnLateUpdate();
		}
#endif
	}
}
