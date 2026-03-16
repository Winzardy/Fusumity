using System;
using Fusumity.Collections;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class TMPFontSizeStateSwitcher<TState> : TMPStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrentToDefault), "Current")]
		private TMPFontSizeState _default;

		[LabelText("State To Size"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Size")]
		[SerializeField]
		private SerializableDictionary<TState, TMPFontSizeState> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var config = _dictionary.GetValueOrDefaultSafe(state, _default);

			_tmp.fontSize = config.fontSize;

			_tmp.enableAutoSizing = config.autoSize;
			_tmp.fontSizeMin      = config.autoSize.value.min;
			_tmp.fontSizeMax      = config.autoSize.value.max;
		}

		protected override void Reset()
		{
			base.Reset();
			SetCurrentToDefault();
		}

		private void SetCurrentToDefault()
		{
			if (_tmp != null)
			{
				_default.fontSize = _tmp.fontSize;

				var minMax = new Range<float>(_tmp.fontSizeMin, _tmp.fontSizeMax);
				_default.autoSize = new Toggle<Range<float>>(minMax, _tmp.enableAutoSizing);
			}
		}
	}

	[Serializable]
	[HideLabel]
	public struct TMPFontSizeState
	{
		[ShowIf(nameof(ShowFontSizeEditor))]
		public float fontSize;

		public Toggle<Range<float>> autoSize;

		private bool ShowFontSizeEditor => !autoSize;
	}
}
