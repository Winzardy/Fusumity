using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ButtonColorStateSwitcher<TState> : StateSwitcher<TState>
	{
		public Button button;

		[SerializeField]
		private ColorBlock _default;

		[LabelText("State To Colors"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Colors")]
		[SerializeField]
		private SerializableDictionary<TState, ColorBlock> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			button.colors = _dictionary.GetValueOrDefault(state, _default);
		}
	}
}
