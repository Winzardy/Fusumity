using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class TMPMaterialStateSwitcher<TState> : TMPStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrentToDefault), "Current")]
		private Material _default;

		[LabelText("State To Material"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Material")]
		[SerializeField]
		private SerializableDictionary<TState, Material> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _tmp.fontMaterial = _dictionary.GetValueOrDefaultSafe(state, _default);

		protected override void Reset()
		{
			base.Reset();
			SetCurrentToDefault();
		}

		private void SetCurrentToDefault()
		{
			if (_tmp != null)
			{
				_default = _tmp.fontMaterial;
			}
		}
	}
}
