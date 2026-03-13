using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class LayoutGroupSpacingStateSwitcher<TState> : LayoutGroupStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrent), "Current")]
		private float _default;

		[SerializeField]
		[LabelText("State To Spacing"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Spacing")]
		private SerializableDictionary<TState, float> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var value = _dictionary.GetValueOrDefaultSafe(state, _default);
			_layoutGroup.spacing = value;
		}

		protected override void Reset()
		{
			base.Reset();
			SetCurrent();
		}

		private void SetCurrent()
		{
			if (_layoutGroup != null)
			{
				_default = _layoutGroup.spacing;
			}
		}
	}
}
