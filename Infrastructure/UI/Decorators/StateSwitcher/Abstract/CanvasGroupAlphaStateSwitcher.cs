using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class CanvasGroupAlphaStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private CanvasGroup _group;

		[SerializeField]
		private float _default;

		[SerializeField]
		[LabelText("State To Color"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Alpha")]
		private SerializableDictionary<TState, float> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _group.alpha = _dictionary.GetValueOrDefaultSafe(state, _default);

		private void Reset()
		{
			if (TryGetComponent(out _group))
				_default = _group.alpha;
		}
	}
}
