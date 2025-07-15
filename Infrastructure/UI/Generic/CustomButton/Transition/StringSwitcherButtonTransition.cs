using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class StringSwitcherButtonTransition : ButtonTransition
	{
		[SerializeField]
		private StateSwitcher<string> _target;

		// TODO: подумать как лучше решать кейс когда нужно вернуть стиль родителя
		[SerializeField]
		private StateSwitcher<string> _parent;

		[SerializeField, DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Style")]
		private SerializableDictionary<ButtonTransitionState, string> _styles;

		public override void DoStateTransition(int state, bool _)
		{
			if (!_target)
				return;

			_target.Switch(_styles.TryGetValue(state, out var style)
				? style
				: _parent.Current);
		}
	}
}
