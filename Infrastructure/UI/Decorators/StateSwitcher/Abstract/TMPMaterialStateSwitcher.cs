using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	public abstract class TMPMaterialStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private TMP_Text _tmp;

		[SerializeField]
		private Material _default;

		[LabelText("State To Material"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Material")]
		[SerializeField]
		private SerializableDictionary<TState, Material> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _tmp.fontMaterial = _dictionary.GetValueOrDefault(state, _default);
	}
}
