using Sapientia.Extensions;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Content.Keys.Editor
{
	/// <summary>
	/// Геометрия строки [Key]-поля в одном месте: и отрисовка суффикса, и решение о переполнении
	/// считаются по одним и тем же ректам, поэтому не могут разъехаться
	/// </summary>
	internal static class KeyDrawerGUI
	{
		// Зазор Unity между зоной лейбла и ячейкой значения (EditorGUI.kPrefixPaddingRight)
		private const float PREFIX_PADDING = 2f;

		// Зазор между значением и лейблом каталога
		private const float SUFFIX_SPACING = 2f;

		// Запас справа под каретку селектора
		private const float SUFFIX_MARGIN = 16f;

		/// <summary>
		/// Ячейка значения внутри строки инспектора. Лейбл рисует сам контрол, а отступ вложенности
		/// Unity съедает зоной лейбла, не двигая значение — поэтому IndentedRect тут был бы лишним
		/// и уводил суффикс на 15px за уровень. Без лейбла контрол сдвигает себя сам, и отступ нужен
		/// </summary>
		public static Rect GetValueRect(Rect rowRect, GUIContent label)
		{
			if (label == null || label.text.IsNullOrEmpty())
				return EditorGUI.IndentedRect(rowRect);

			var valueRect = rowRect;
			valueRect.xMin += EditorGUIUtility.labelWidth + PREFIX_PADDING;
			return valueRect;
		}

		/// <summary>
		/// Рект лейбла каталога справа от значения. Конец значения спрашиваем у самого стиля поля:
		/// CalcWidth не знает про padding и выравнивание, и на коротких значениях суффикс уползал
		/// </summary>
		public static Rect GetSuffixRect(Rect rowRect, GUIContent label, string value, string suffixText,
			GUIStyle valueStyle)
		{
			var valueRect = GetValueRect(rowRect, label);
			var content = new GUIContent(value);
			var valueEndX = valueStyle.GetCursorPixelPosition(valueRect, content, content.text.Length).x;

			var suffixRect = valueRect;
			suffixRect.xMin = valueEndX + SUFFIX_SPACING;
			suffixRect.width = EditorStyles.label.CalcWidth(suffixText);
			return suffixRect;
		}

		public static void DrawSuffix(Rect rowRect, GUIContent label, string value, string suffixText,
			GUIStyle valueStyle) =>
			GUI.Label(GetSuffixRect(rowRect, label, value, suffixText, valueStyle), suffixText, EditorStyles.label);

		/// <summary>
		/// Влезает ли лейбл каталога рядом со значением. Запас справа — под каретку селектора
		/// </summary>
		public static bool FitsSuffix(Rect rowRect, GUIContent label, string value, string suffixText,
			GUIStyle valueStyle) =>
			GetSuffixRect(rowRect, label, value, suffixText, valueStyle).xMax <= rowRect.xMax - SUFFIX_MARGIN;
	}
}
