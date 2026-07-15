using System;
using JetBrains.Annotations;
using Sapientia.Conditions;
using UnityEngine;

namespace UI
{
	public class BooleanStateSwitcherCombiner : ActivationBehaviour
	{
		[SerializeField]
		private bool _invert;

		[SerializeField]
		private bool _invertA;

		[NotNull]
		[SerializeField]
		private StateSwitcher<bool> _a;

		[SerializeField]
		private LogicalOperator _operator;

		[SerializeField]
		private bool _invertB;

		[NotNull]
		[SerializeField]
		private StateSwitcher<bool> _b;

		[Space]
		[NotNull]
		[SerializeField]
		private StateSwitcher<bool> _switcher;

		protected override void OnEnabled()
		{
			Subscribe();
			Refresh(true);
		}

		protected override void OnDisabled()
		{
			Unsubscribe();
		}

		private void Subscribe()
		{
			_a.StateSwitched += OnSourceStateSwitched;
			_b.StateSwitched += OnSourceStateSwitched;
		}

		private void Unsubscribe()
		{
			_a.StateSwitched -= OnSourceStateSwitched;
			_b.StateSwitched -= OnSourceStateSwitched;
		}

		private void OnSourceStateSwitched(bool _, bool immediate) => Refresh(immediate);

		private void Refresh(bool immediate)
		{
			if (_a == null || _b == null)
				return;

			var a = _invertA ? !_a.Current : _a.Current;
			var b = _invertB ? !_b.Current : _b.Current;

			var state = _operator switch
			{
				LogicalOperator.Or => a || b,
				LogicalOperator.And => a && b,
				_ => throw new ArgumentOutOfRangeException()
			};

			if (_invert)
				state = !state;

			_switcher.Switch(state, immediate);
		}

		private void OnValidate()
		{
			if (_a != null && ReferenceEquals(_a, _b))
			{
				Debug.LogError("Can't set the same state switcher as both sources");
				_b = null!;
			}
		}
	}
}
