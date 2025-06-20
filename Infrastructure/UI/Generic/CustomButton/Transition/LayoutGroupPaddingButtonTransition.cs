using System;
using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(LayoutGroup))]
	public class LayoutGroupPaddingButtonTransition : ButtonTransition
	{
		[SerializeField]
		private LayoutGroup _target;

		[SerializeField, DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Padding")]
		private SerializableDictionary<ButtonTransitionState, SerializableRectOffset> _states;

		public override void DoStateTransition(int state, bool instant)
		{
			if (!_target)
				return;

			_target.padding = _states.TryGetValue(state, out var rectOffset)
				? rectOffset.ToRectOffset()
				: _states[ButtonTransitionType.NORMAL].ToRectOffset();
		}

		private void Reset()
		{
			TryGetComponent(out _target);
			_states = new(new[]
				{new KeyValuePair<ButtonTransitionState, SerializableRectOffset>(ButtonTransitionType.NORMAL, _target.padding)});
		}
	}

	/// <summary>
	/// Собственная структура, так как если использовать RectOffset -> крашит редактор...
	/// </summary>
	[Serializable]
	[HideLabel]
	public struct SerializableRectOffset
	{
		public int left;
		public int right;
		public int top;
		public int bottom;

		public SerializableRectOffset(int left, int right, int top, int bottom)
		{
			this.left = left;
			this.right = right;
			this.top = top;
			this.bottom = bottom;
		}

		public RectOffset ToRectOffset() => new(left, right, top, bottom);

		public static SerializableRectOffset FromRectOffset(RectOffset offset)
			=> new(offset.left, offset.right, offset.top, offset.bottom);

		public static implicit operator RectOffset(SerializableRectOffset rectOffset) => rectOffset.ToRectOffset();
		public static implicit operator SerializableRectOffset(RectOffset rectOffset) => FromRectOffset(rectOffset);
	}
}
