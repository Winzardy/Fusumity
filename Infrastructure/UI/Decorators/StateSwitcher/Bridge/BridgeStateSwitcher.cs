using Fusumity.Attributes.Odin;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Bridge
{
	public abstract class BridgeStateSwitcher<TState, TLinkedState> : StateSwitcher<TState>
	{
		[SerializeField]
		private StateSwitcher<TLinkedState> _switcher;

		[SerializeField, InlineToggle(nameof(_ignoreDefault), "Ignore", margins = 5)]
		private TLinkedState _default;

		/// <summary>
		/// If enabled - default TState and states that are not assigned in the dictionary -
		/// will be completely ignored, the fallback (_default) state will not be used.
		/// </summary>
		[SerializeField, HideInInspector]
		private bool _ignoreDefault;

		[SerializeField]
		[InfoBox("Null, default and non-assigned states will be ignored", InfoMessageType.Warning, VisibleIf = nameof(_ignoreDefault))]
		[LabelText("State To Linked State"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Linked State")]
		private SerializableDictionary<TState, TLinkedState> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			if (TryRetrieveLinkedState(state, out var linkedState))
			{
				_switcher.Switch(linkedState);
			}
		}

		private bool TryRetrieveLinkedState(TState providedState, out TLinkedState linkedState)
		{
			linkedState = default;

			if (_ignoreDefault)
			{
				if (EqualityComparer<TState>.Default.Equals(providedState, default))
					return false;

				return _dictionary.TryGetValue(providedState, out linkedState);
			}

			linkedState = _dictionary.GetValueOrDefaultSafe(providedState, _default);
			return true;
		}
	}
}
