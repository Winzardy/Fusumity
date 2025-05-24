using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ButtonTransitionSpritesStateSwitcher<TState> : StateSwitcher<TState>
	{
		public ImageSpriteButtonTransition spriteTransition;

		[SerializeField]
		private SpriteState _default;

		[LabelText("State To Sprites"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Sprites")]
		[SerializeField]
		private SerializableDictionary<TState, SpriteState> _dictionary;

		protected override void OnStateSwitched(TState state)
			=> spriteTransition.SetState(_dictionary.GetValueOrDefault(state, _default));
	}
}
