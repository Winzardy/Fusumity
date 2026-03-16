using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class GraphicAlphaStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private Graphic _graphic;

		[SerializeField]
		private float _default;

		[SerializeField]
		[LabelText("State To Alpha"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Alpha")]
		private SerializableDictionary<TState, float> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _graphic.SetAlpha(_dictionary.GetValueOrDefaultSafe(state, _default));
	}
}
