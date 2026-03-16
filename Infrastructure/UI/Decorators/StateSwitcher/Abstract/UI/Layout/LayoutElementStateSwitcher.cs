using System;
using Fusumity.Collections;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public abstract class LayoutElementStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private LayoutElement _element;

		[SerializeField]
		[InlineButton(nameof(SetCurrent), "Current")]
		private LayoutElementState _default;

		[SerializeField]
		[LabelText("State To Element State"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Element State")]
		private SerializableDictionary<TState, LayoutElementState> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var value = _dictionary.GetValueOrDefaultSafe(state, _default);
			value.Apply(_element);
		}

		protected virtual void Reset()
		{
			_element = GetComponent<LayoutElement>();

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
	public struct LayoutElementState
	{
		public bool ignoreLayout;
		public Toggle<float> minWidth;
		public Toggle<float> minHeight;
		public Toggle<float> preferredWidth;
		public Toggle<float> preferredHeight;
		public Toggle<float> flexibleWidth;
		public Toggle<float> flexibleHeight;
		public int layoutPriority;

		public static implicit operator LayoutElementState(LayoutElement element)
		{
			return new LayoutElementState
			{
				ignoreLayout = element.ignoreLayout,

				minWidth        = element.minWidth >= 0 ? element.minWidth : new Toggle<float>(0, false),
				minHeight       = element.minHeight >= 0 ? element.minHeight : new Toggle<float>(0, false),
				preferredWidth  = element.preferredWidth >= 0 ? element.preferredWidth : new Toggle<float>(0, false),
				preferredHeight = element.preferredHeight >= 0 ? element.preferredHeight : new Toggle<float>(0, false),
				flexibleWidth   = element.flexibleWidth >= 0 ? element.flexibleWidth : new Toggle<float>(0, false),
				flexibleHeight  = element.flexibleHeight >= 0 ? element.flexibleHeight : new Toggle<float>(0, false),

				layoutPriority = element.layoutPriority
			};
		}

		public void Apply(LayoutElement element)
		{
			element.ignoreLayout = ignoreLayout;

			element.minWidth        = minWidth ? minWidth : -1;
			element.minHeight       = minHeight ? minHeight : -1;
			element.preferredWidth  = preferredWidth ? preferredWidth : -1;
			element.preferredHeight = preferredHeight ? preferredHeight : -1;
			element.flexibleWidth   = flexibleWidth ? flexibleWidth : -1;
			element.flexibleHeight  = flexibleHeight ? flexibleHeight : -1;

			element.layoutPriority = layoutPriority;
		}
	}
}
