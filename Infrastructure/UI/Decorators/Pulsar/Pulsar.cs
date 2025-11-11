using Fusumity.Attributes.Odin;
using Fusumity.Reactive;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Задача <see cref="Pulsar"/> выдавать нормализованное значение от 0 до 1 глобально, да так чтобы все "мигающие" активности были синхронные
	/// </summary>
#if UNITY_EDITOR
	[ExecuteAlways]
#endif
	public abstract class Pulsar : MonoBehaviour
	{
		[SerializeField]
		[Minimum(0), Unit(Units.Second)]
		protected float _interval = 1;

		[SerializeField]
		private AnimationCurve _curve = AnimationCurve.Linear(0, 0, 1, 1);

		[MinMaxSlider(0, 1, true)]
		[SerializeField]
		private Vector2 _range = new Vector2(0, 1);

		private bool _subscribed;

		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_subscribed = true;
			UnityLifecycle.UpdateEvent.Subscribe(OnUpdate);
			OnEnabled();
			OnUpdate(0);
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_subscribed = false;
			UnityLifecycle.UpdateEvent.UnSubscribe(OnUpdate);
			OnDisabled();
			OnUpdate(0);
		}

		protected virtual void OnEnabled()
		{
		}

		protected virtual void OnDisabled()
		{
		}

		private void OnUpdate()
		{
			var remainder = Time.realtimeSinceStartup % _interval;
			var normalizedValue = remainder / _interval;
			var f = _curve.Evaluate(normalizedValue);
			var value = _range.x + (f * (_range.y - _range.x));
			OnUpdate(value);
		}

		protected abstract void OnUpdate(float normalizedValue);

#if UNITY_EDITOR
		private void Update()
		{
			if (Application.isPlaying)
				return;

			OnUpdate();
		}
#endif
	}
}
