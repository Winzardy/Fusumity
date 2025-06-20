using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class GraphicColorStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private Graphic _graphic;

		[SerializeField]
		private Color _default;

		[SerializeField]
		[LabelText("State To Color"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Color")]
		private SerializableDictionary<TState, Color> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _graphic.color = _dictionary.GetValueOrDefault(state, _default);
	}
}
