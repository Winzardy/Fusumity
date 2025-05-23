using System;
using System.Collections.Generic;
using TMPro;

namespace UI
{
	public class UIDropdownWidget<T> : UIWidget<UIDropdownWidgetLayout>
	{
		private Dictionary<int, T> _valuesByIndex;

		public event Action<int> ValueChanged;

		protected override void OnLayoutInstalled()
		{
			_layout.dropdown.onValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnLayoutCleared()
		{
			_layout.dropdown.onValueChanged.RemoveListener(OnValueChanged);
		}

		public void SetOptions(IReadOnlyList<string> variants, Func<string, T> toValue, Func<T, string> toString)
		{
			_valuesByIndex = new Dictionary<int, T>();

			var options = new List<T>();
			for (int i = 0; i < variants.Count; i++)
			{
				var variant = variants[i];
				var value = toValue.Invoke(variant);

				_valuesByIndex[i] = value;

				options.Add(value);
			}

			_layout.dropdown.options = CollectStringValues(options, toString);
		}

		public void SetOptions(IReadOnlyList<T> variants, Func<T, string> toString = null)
		{
			SetOptions(variants, -1, toString);
		}

		public void SetOptions(IReadOnlyList<T> variants, int defaultIndex, Func<T, string> toString = null)
		{
			_valuesByIndex = new Dictionary<int, T>();

			var options = new List<T>();
			for (int i = 0; i < variants.Count; i++)
			{
				var value = variants[i];

				_valuesByIndex[i] = value;

				options.Add(value);
			}

			_layout.dropdown.options = CollectStringValues(options, toString);

			if (defaultIndex >= 0)
			{
				_layout.dropdown.value = defaultIndex;
			}
		}

		private List<TMP_Dropdown.OptionData> CollectStringValues(IReadOnlyList<T> variants,
			Func<T, string> toString = null)
		{
			var result = new List<TMP_Dropdown.OptionData>();

			foreach (var value in variants)
			{
				var text = string.Empty;

				if (value is string s)
				{
					text = s;
				}

				if (toString != null)
				{
					text = toString.Invoke(value);
				}

				result.Add(new TMP_Dropdown.OptionData {text = text,});
			}

			return result;
		}

		public T GetTargetValue()
		{
			var index = _layout.dropdown.value;
			return _valuesByIndex[index];
		}

		public void TrySetValue(T value, int defaultValue = 0)
		{
			if (value == null)
			{
				_layout.dropdown.value = defaultValue;
				return;
			}

			foreach (var pair in _valuesByIndex)
			{
				if (Equals(pair.Value, value))
				{
					_layout.dropdown.value = pair.Key;
					return;
				}
			}

			_layout.dropdown.value = defaultValue;
		}

		private void OnValueChanged(int valueI)
		{
			ValueChanged?.Invoke(valueI);
		}
	}
}
