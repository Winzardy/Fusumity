using System.Collections.Generic;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class ImageStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private Image _image;

		[SerializeField]
		private Sprite _default;

		[LabelText("State To Sprite"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sprite")]
		[SerializeField]
		private SerializableDictionary<TState, Sprite> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _image.sprite = _dictionary.GetValueOrDefault(state, _default);
	}
}
