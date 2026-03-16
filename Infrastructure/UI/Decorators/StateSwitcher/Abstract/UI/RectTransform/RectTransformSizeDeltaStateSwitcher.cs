using Fusumity.Collections;
using Sapientia;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public abstract class RectTransformSizeDeltaStateSwitcher<TState> : StateSwitcher<TState>
	{
		[SerializeField]
		private RectTransform _rectTransform;

		[SerializeField]
		[InlineButton(nameof(SetCurrent), "Current")]
		private OptionalVector2 _default;

		[SerializeField]
		[LabelText("State To Size Delta"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Size Delta")]
		private SerializableDictionary<TState, OptionalVector2> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var value = _dictionary.GetValueOrDefaultSafe(state, _default);

			if (value.x)
				_rectTransform.sizeDelta = new Vector2(value.x, _rectTransform.sizeDelta.y);
			if (value.y)
				_rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, value.y);
		}

		protected virtual void Reset()
		{
			_rectTransform = GetComponent<RectTransform>();

			SetCurrent();
		}

		private void SetCurrent()
		{
			if (_rectTransform != null)
			{
				_default = new OptionalVector2(_rectTransform.sizeDelta.x, _rectTransform.sizeDelta.y);
			}
		}
	}
}
