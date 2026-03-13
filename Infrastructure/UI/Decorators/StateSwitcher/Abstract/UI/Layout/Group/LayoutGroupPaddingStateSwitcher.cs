using System;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class LayoutGroupPaddingStateSwitcher<TState> : LayoutGroupStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrent), "Current")]
		private RectOffsetData _default;

		[SerializeField]
		[LabelText("State To Padding"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Padding")]
		private SerializableDictionary<TState, RectOffsetData> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var value = _dictionary.GetValueOrDefaultSafe(state, _default);
			_layoutGroup.padding = value.ToRectOffset();
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
				_default = RectOffsetData.From(_layoutGroup.padding);
			}
		}
	}

	[Serializable]
	public struct RectOffsetData
	{
		public int left;
		public int right;
		public int top;
		public int bottom;

		public RectOffset ToRectOffset() => new(left, right, top, bottom);

		public static RectOffsetData From(RectOffset r)
		{
			return new RectOffsetData
			{
				left   = r.left,
				right  = r.right,
				top    = r.top,
				bottom = r.bottom
			};
		}
	}
}
