using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class GameObjectActiveStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private bool _default;

		[LabelText("State To Enable"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Enable")]
		[SerializeField]
		protected SerializableDictionary<TState, bool> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> gameObject.SetActive(GetState(state));

		protected bool GetState(TState state)
			=> _dictionary?.GetValueOrDefaultSafe(state, _default) ?? _default;

		protected void SetDefaultActive(bool active)
			=> _default = active;
	}
}
