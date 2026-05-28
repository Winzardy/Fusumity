using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class TransformRotationSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private Transform _transform;

		[SerializeField]
		private Vector3 _default = Vector3.one;

		[SerializeField]
		[LabelText("State To Rotation"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Rotation")]
		private SerializableDictionary<TState, Vector3> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _transform.rotation = Quaternion.Euler(_dictionary.GetValueOrDefaultSafe(state, _default));
	}
}
