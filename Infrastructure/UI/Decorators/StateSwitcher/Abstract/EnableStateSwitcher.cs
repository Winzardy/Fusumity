using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class EnableStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private bool _default;

		[LabelText("State To Enable"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Enable")]
		[SerializeField]
		private SerializableDictionary<TState, bool> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> gameObject.SetActive(_dictionary.GetValueOrDefault(state, _default));
	}
}
