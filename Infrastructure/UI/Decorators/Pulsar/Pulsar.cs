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

		[SerializeField, BoxGroup]
		private bool _updateInEditMode = true;
		[SerializeField, BoxGroup]
		[Tooltip("Use Time.realtimeSinceStartup instead of the local tick when updating, to sync with other pulsars.")]
		private bool _useGlobalTick = true;
		[SerializeField, BoxGroup, ShowIf("@this._useGlobalTick == false")]
		private bool _resetTickOnEnable = true;
		[SerializeField, BoxGroup]
		[Tooltip("Reset value to 0 instead of the lower range on disabling.")]
		private bool _resetValueToZero;

		private bool _subscribed;
		private float _localTick;

		private void OnEnable() => TrySubscribe();

		private void OnDisable() => TryUnsubscribe();

		private void OnDestroy() => TryUnsubscribe();

		private void TrySubscribe()
		{
			if (_subscribed)
				return;

			_localTick = 0;
			_subscribed = true;
			UnityLifecycle.UpdateEvent.Subscribe(OnUpdate);
			OnEnabled();
			SetInitialValue();
		}

		private void TryUnsubscribe()
		{
			if (!_subscribed)
				return;

			_localTick = 0;
			_subscribed = false;
			UnityLifecycle.UpdateEvent.UnSubscribe(OnUpdate);
			OnDisabled();

			if (_resetValueToZero)
			{
				OnUpdate(0);
			}
			else
			{
				SetInitialValue();
			}
		}

		protected virtual void OnEnabled()
		{
		}

		protected virtual void OnDisabled()
		{
		}

		private void OnUpdate()
		{
			if (!_useGlobalTick)
			{
				_localTick += Time.deltaTime;
			}

			var time = _useGlobalTick ? Time.realtimeSinceStartup : _localTick;

			var remainder = time % _interval;
			var normalizedValue = remainder / _interval;
			var f = _curve.Evaluate(normalizedValue);
			var value = _range.x + (f * (_range.y - _range.x));
			OnUpdate(value);
		}

		private void SetInitialValue()
		{
			var f = _curve.Evaluate(0);
			var value = _range.x + (f * (_range.y - _range.x));
			OnUpdate(value);
		}

		protected abstract void OnUpdate(float normalizedValue);

#if UNITY_EDITOR
		private void Update()
		{
			if (_updateInEditMode && !Application.isPlaying)
			{
				OnUpdate();
			}
		}
#endif
	}
}
