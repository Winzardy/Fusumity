using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class AnchoredPositionSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private RectTransform _transform;

		[SerializeField]
		private Vector3 _default = Vector3.one;

		[SerializeField]
		[LabelText("State To Anchored Pos"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Anchored Pos")]
		private SerializableDictionary<TState, Vector3> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _transform.anchoredPosition = _dictionary.GetValueOrDefaultSafe(state, _default);
	}
}
