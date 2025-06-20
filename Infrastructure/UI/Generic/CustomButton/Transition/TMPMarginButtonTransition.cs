using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	[RequireComponent(typeof(TMP_Text))]
	public class TMPMarginButtonTransition : ButtonTransition
	{
		[SerializeField]
		private TMP_Text _target;

		[SerializeField, DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Margin")]
		private SerializableDictionary<ButtonTransitionState, Vector4> _states;

		public override void DoStateTransition(int state, bool instant)
		{
			if (!_target)
				return;

			_target.margin = _states.TryGetValue(state, out var vector)
				? vector
				: _states[ButtonTransitionType.NORMAL];
		}

		private void Reset()
		{
			TryGetComponent(out _target);
			_states = new(new[]
				{new KeyValuePair<ButtonTransitionState, Vector4>(ButtonTransitionType.NORMAL, _target.margin)});
		}
	}
}
