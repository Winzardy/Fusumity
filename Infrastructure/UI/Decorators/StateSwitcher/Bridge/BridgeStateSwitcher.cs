using System.Collections.Generic;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Bridge
{
	public abstract class BridgeStateSwitcher<TState, TLinkedState> : StateSwitcher<TState>
	{
		[SerializeField]
		private StateSwitcher<TLinkedState> _switcher;

		[SerializeField]
		private TLinkedState _default;

		[SerializeField]
		[LabelText("State To Linked State"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Linked State")]
		private SerializableDictionary<TState, TLinkedState> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var linkedState = _dictionary.GetValueOrDefaultSafe(state, _default);
			_switcher.Switch(linkedState);
		}
	}
}
