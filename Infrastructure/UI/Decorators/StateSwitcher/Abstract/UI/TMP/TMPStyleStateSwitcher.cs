using System;
using System.Collections.Generic;
using Fusumity.Collections;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	public abstract class TMPStyleStateSwitcher<TState> : TMPStateSwitcher<TState>
	{
		[SerializeField]
		[InlineButton(nameof(SetCurrentToDefault), "Current")]
		private TMPStyle _default;

		[LabelText("State To Style"), DictionaryDrawerSettings(KeyLabel = "State", ValueLabel = "Style")]
		[SerializeField]
		private SerializableDictionary<TState, TMPStyle> _dictionary;

		protected override void OnStateSwitched(TState state)
		{
			var style = _dictionary.GetValueOrDefaultSafe(state, _default);
			_tmp.textStyle = style;
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
				_default = _tmp.textStyle.name;
			}
		}
	}

	[HideLabel]
	[Serializable]
	public struct TMPStyle
	{
		[ValueDropdown(nameof(GetStyles))]
		public string name;

		public static implicit operator string(TMPStyle style) => style.name;
		public static implicit operator TMP_Style(TMPStyle style) => GetStyle(style.name);

		public static implicit operator TMPStyle(string name) => new()
		{
			name = name
		};

		private static TMP_Style GetStyle(string styleName)
		{
			if (string.IsNullOrEmpty(styleName))
				return null;

			var sheet = TMP_Settings.defaultStyleSheet;
			if (sheet == null)
				return null;

			return sheet.GetStyle(styleName);
		}

		private static IEnumerable<string> GetStyles()
		{
			var sheet = TMP_Settings.defaultStyleSheet;
			if (sheet == null)
				yield break;

#if UNITY_EDITOR
			var so = new UnityEditor.SerializedObject(sheet);
			var prop = so.FindProperty("m_StyleList");

			for (int i = 0; i < prop.arraySize; i++)
			{
				var element = prop.GetArrayElementAtIndex(i);
				var name = element.FindPropertyRelative("m_Name").stringValue;
				yield return name;
			}
#endif
		}
	}
}
