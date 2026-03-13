using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class ImageSpriteStateSwitcher<TState> : ImageStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrentToDefault), "Current")]
		private Sprite _default;

		[LabelText("State To Sprite"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sprite")]
		[SerializeField]
		private SerializableDictionary<TState, Sprite> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> _image.sprite = _dictionary.GetValueOrDefaultSafe(state, _default);

		protected override void Reset()
		{
			base.Reset();
			SetCurrentToDefault();
		}

		private void SetCurrentToDefault()
		{
			if (_image != null)
			{
				_default = _image.sprite;
			}
		}
	}
}
