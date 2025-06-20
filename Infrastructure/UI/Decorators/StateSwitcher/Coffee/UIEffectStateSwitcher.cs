using System.Collections.Generic;
using Coffee.UIEffects;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Coffee
{
	public class UIEffectStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private UIEffect _effect;

		[SerializeField]
		private UIEffect _default;

		[LabelText("State To Preset"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Preset")]
		[SerializeField]
		private SerializableDictionary<TState, UIEffect> _dictionary;

		protected override void OnStateSwitched(TState state) =>
			_effect.LoadPreset(_dictionary.GetValueOrDefault(state, _default));
	}
}
