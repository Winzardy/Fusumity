using System;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	/// <summary>
	/// Рисует байтовое поле тремя под-полями MB / KB / B, которые собираются в одно значение:
	/// value = mb * 1024^2 + kb * 1024 + b. Переполнение под-поля (например 2048 KB) нормализуется
	/// в старший разряд при следующей отрисовке. Значение не ограничивается — диапазон задаётся
	/// отдельными атрибутами (Minimum/Maximum).
	/// </summary>
	[DrawerPriority(0, 8000, 0)]
	public class ByteSizeDrawer<T> : OdinAttributeDrawer<ByteSizeAttribute, T>
		where T : struct
	{
		private const float UNIT_WIDTH = 24f;
		private const float SPACING = 4f;

		private static readonly bool IsNumber = GenericNumberUtility.IsNumber(typeof(T));

		public override bool CanDrawTypeFilter(Type type)
		{
			return IsNumber;
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var value = Convert.ToInt64(this.ValueEntry.SmartValue);
			var megabytes = value / (1024L * 1024L);
			var kilobytes = value / 1024L % 1024L;
			var bytes = value % 1024L;

			var rect = EditorGUILayout.GetControlRect();
			if (label != null)
				rect = EditorGUI.PrefixLabel(rect, label);

			var segmentWidth = (rect.width - SPACING * 2f) / 3f;

			EditorGUI.BeginChangeCheck();
			megabytes = DrawPart(new Rect(rect.x, rect.y, segmentWidth, rect.height), megabytes, "MB");
			kilobytes = DrawPart(new Rect(rect.x + segmentWidth + SPACING, rect.y, segmentWidth, rect.height), kilobytes, "KB");
			bytes = DrawPart(new Rect(rect.x + (segmentWidth + SPACING) * 2f, rect.y, segmentWidth, rect.height), bytes, "B");
			if (EditorGUI.EndChangeCheck())
			{
				var packed = megabytes * (1024L * 1024L) + kilobytes * 1024L + bytes;
				this.ValueEntry.SmartValue = (T)Convert.ChangeType(packed, typeof(T));
			}
		}

		private static long DrawPart(Rect rect, long value, string unit)
		{
			var fieldRect = rect;
			fieldRect.xMax -= UNIT_WIDTH;
			var unitRect = rect;
			unitRect.xMin = rect.xMax - UNIT_WIDTH + 2f;

			var result = EditorGUI.LongField(fieldRect, value);
			GUI.Label(unitRect, unit, EditorStyles.miniLabel);
			return result;
		}
	}
}
