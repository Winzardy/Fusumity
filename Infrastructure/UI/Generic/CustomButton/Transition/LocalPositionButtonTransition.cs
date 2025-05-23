using System.Collections.Generic;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	[RequireComponent(typeof(Transform))]
	public class LocalPositionButtonTransition : ButtonTransition
	{
		[SerializeField]
		private Transform _target;

		[SerializeField, DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Position")]
		private SerializableDictionary<ButtonTransitionState, Vector3> _positions;

		public override void DoStateTransition(int state, bool _)
		{
			if (!_target)
				return;

			_target.localPosition = _positions.TryGetValue(state, out var position) ? position : _positions[ButtonTransitionType.NORMAL];
		}

		private void Reset()
		{
			TryGetComponent(out _target);
			_positions = new(new[]
				{new KeyValuePair<ButtonTransitionState, Vector3>(ButtonTransitionType.NORMAL, transform.localPosition)});
		}
	}
}
