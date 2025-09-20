using Sapientia.Deterministic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Deterministic
{
	public class Fix64PropertyDrawer : OdinValueDrawer<Fix64>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var rect = EditorGUILayout.GetControlRect();

			var delayedPropertyAttribute = ValueEntry.Property.GetAttribute<DelayedPropertyAttribute>();
			var value = (float) ValueEntry.SmartValue;
			if (delayedPropertyAttribute != null)
			{
				value = SirenixEditorFields.DelayedFloatField(rect, label, value);
			}
			else
			{
				value = SirenixEditorFields.FloatField(rect, label, value);
			}

			var display = GetValue(value);

			var fix64 = (Fix64) display;

			GUI.Label(
				rect.HorizontalPadding(0.0f, 8f),
				"raw: " + fix64.RawValue,
				SirenixGUIStyles.RightAlignedGreyMiniLabel);

			ValueEntry.SmartValue = fix64;
		}

		private float GetValue(float value)
		{
			var minAttribute = ValueEntry.Property.GetAttribute<MinValueAttribute>();
			var maxAttribute = ValueEntry.Property.GetAttribute<MaxValueAttribute>();

			if (minAttribute != null)
			{
				var minValue = ValueResolver.Get<double>(this.Property, minAttribute.Expression, minAttribute.MinValue).GetValue();
				if (value < minValue)
				{
					return (float) minValue;
				}
			}

			if (maxAttribute != null)
			{
				var maxValue = ValueResolver.Get<double>(this.Property, maxAttribute.Expression, maxAttribute.MaxValue).GetValue();

				if (value > maxValue)
				{
					return (float) maxValue;
				}
			}

			return value;
		}
	}
}
