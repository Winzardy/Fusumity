using System;
using Fusumity.Collections;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class CustomLayoutElementStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private CustomLayoutElement _element;

		[SerializeField]
		[InlineButton(nameof(SetCurrent), "Current")]
		private CustomLayoutElementState _default;

		[SerializeField]
		[LabelText("State To Element State"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Element State")]
		private SerializableDictionary<TState, CustomLayoutElementState> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var value = _dictionary.GetValueOrDefaultSafe(state, _default);
			value.Apply(_element);
		}

		private void Reset()
		{
			_element = GetComponent<CustomLayoutElement>();
			SetCurrent();
		}

		private void SetCurrent()
		{
			if (_element != null)
			{
				_default = _element;
			}
		}
	}

	[Serializable]
	public struct CustomLayoutElementState
	{
		public Toggle<CustomLayoutSize> maxWidth;
		public Toggle<CustomLayoutSize> minWidth;
		public Toggle<CustomLayoutSize> maxHeight;
		public Toggle<CustomLayoutSize> minHeight;

		public LayoutElementState baseState;

		public static implicit operator CustomLayoutElementState(CustomLayoutElement element)
		{
			return new CustomLayoutElementState
			{
				baseState = element,
				maxWidth = new Toggle<CustomLayoutSize>(new CustomLayoutSize
				{
					size          = element.maxWidth,
					rectTransform = element.MaxWidthRect
				}, element.maxWidth >= 0),
				minWidth = new Toggle<CustomLayoutSize>(new CustomLayoutSize
				{
					size          = element.minWidth,
					rectTransform = element.MinWidthRect
				}, element.minWidth >= 0),
				maxHeight = new Toggle<CustomLayoutSize>(new CustomLayoutSize
				{
					size          = element.maxHeight,
					rectTransform = element.MaxHeightRect
				}, element.maxHeight >= 0),
				minHeight = new Toggle<CustomLayoutSize>(new CustomLayoutSize
				{
					size          = element.minHeight,
					rectTransform = element.MinHeightRect
				}, element.minHeight >= 0),
			};
		}

		public void Apply(CustomLayoutElement element)
		{
			baseState.Apply(element);

			if (maxWidth)
			{
				element.maxWidth     = maxWidth ? maxWidth.value.size : 0;
				element.MaxWidthRect = maxWidth.value.rectTransform;
			}

			if (minWidth)
			{
				element.minWidth     = minWidth ? minWidth.value.size : 0;
				element.MinWidthRect = minWidth.value.rectTransform;
			}

			if (maxHeight)
			{
				element.maxHeight     = maxHeight ? maxHeight.value.size : 0;
				element.MaxHeightRect = maxHeight.value.rectTransform;
			}

			if (minHeight)
			{
				element.minHeight     = minHeight ? minHeight.value.size : 0;
				element.MinHeightRect = minHeight.value.rectTransform;
			}
		}
	}

	[Serializable]
	[HideLabel]
	public struct CustomLayoutSize
	{
		public float size;
		public RectTransform rectTransform;
	}
}
