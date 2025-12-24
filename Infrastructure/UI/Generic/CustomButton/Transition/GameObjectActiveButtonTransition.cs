using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class GameObjectActiveButtonTransition : ButtonTransition
	{
		[SerializeField]
		private GameObject _target;

		[SerializeField, DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Active")]
		private SerializableDictionary<ButtonTransitionState, bool> _states;

		public override void DoStateTransition(int state, bool instant)
		{
			if (!_target)
				return;

			_target.SetActive(_states.TryGetValue(state, out var active)
				? active
				: _states[ButtonTransitionType.NORMAL]);
		}
	}
}
