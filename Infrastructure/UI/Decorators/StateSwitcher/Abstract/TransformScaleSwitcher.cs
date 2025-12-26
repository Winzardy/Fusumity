using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class TransformScaleSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private Transform _transform;

		[SerializeField]
		private Vector3 _default = Vector3.one;

		[SerializeField]
		[LabelText("State To Scale"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Scale")]
		private SerializableDictionary<TState, Vector3> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _transform.localScale = _dictionary.GetValueOrDefaultSafe(state, _default);
	}
}
