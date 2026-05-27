using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Задача <see cref="Pulsar"/> выдавать нормализованное значение от 0 до 1 глобально, да так чтобы все "мигающие" активности были синхронные
	/// </summary>
	public abstract class Pulsar : Updatable
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
		[Tooltip("Use Time.realtimeSinceStartup instead of the local tick when updating, to sync with other pulsars.")]
		private bool _useGlobalTick = true;

		[SerializeField, BoxGroup, ShowIf("@this._useGlobalTick == false")]
		private bool _resetTickOnEnable = true;

		[SerializeField, BoxGroup]
		[Tooltip("Reset value to 0 instead of the lower range on disabling.")]
		private bool _resetValueToZero;

		private float _localTick;

		protected override void OnEnabled()
		{
			_localTick = 0;
		}

		protected override void OnDisabled()
		{
			_localTick = 0;

			if (_resetValueToZero)
			{
				OnUpdate(0);
			}
			else
			{
				SetInitialValue();
			}
		}

		protected override void OnUpdate()
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
	}
}
