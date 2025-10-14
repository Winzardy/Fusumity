using System;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia.Deterministic;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Deterministic
{
	public class Fix64PropertyDrawer : OdinValueDrawer<Fix64>
	{
		private const string FORMAT = "raw: {0}";
		private static readonly Color _rawSuffixLabelColor = Color.gray.WithAlpha(0.6f);

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

			FusumityEditorGUILayout.SuffixLabel(FORMAT.Format(fix64.RawValue), true, _rawSuffixLabelColor);
			ValueEntry.SmartValue = fix64;
		}

		private float GetValue(float value)
		{
			var minAttribute = ValueEntry.Property.GetAttribute<MinValueAttribute>();
			var maxAttribute = ValueEntry.Property.GetAttribute<MaxValueAttribute>();

			if (minAttribute != null)
			{
				var minValue = ValueResolver.Get(Property, minAttribute.Expression, minAttribute.MinValue).GetValue();
				if (value < minValue)
				{
					return (float) minValue;
				}
			}

			if (maxAttribute != null)
			{
				var maxValue = ValueResolver.Get(Property, maxAttribute.Expression, maxAttribute.MaxValue).GetValue();

				if (value > maxValue)
				{
					return (float) maxValue;
				}
			}

			return value;
		}
	}

	public class Fix64PropertyRangeAttributeDrawer : OdinAttributeDrawer<PropertyRangeAttribute, Fix64>
	{
		protected ValueResolver<double> getterMinValue;
		protected ValueResolver<double> getterMaxValue;

		/// <summary>Initialized the drawer.</summary>
		protected override void Initialize()
		{
			if (Attribute.MinGetter != null)
				getterMinValue = ValueResolver.Get<double>(Property, Attribute.MinGetter);
			if (Attribute.MaxGetter == null)
				return;
			getterMaxValue = ValueResolver.Get<double>(Property, Attribute.MaxGetter);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var a = getterMinValue?.GetValue() ?? (float) Attribute.Min;
			var b = getterMaxValue?.GetValue() ?? (float) Attribute.Max;
			if (getterMinValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(getterMinValue.ErrorMessage);
			if (getterMaxValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(getterMaxValue.ErrorMessage);
			EditorGUI.BeginChangeCheck();
			var value = SirenixEditorFields.RangeFloatField(label, ValueEntry.SmartValue, (float) Math.Min(a, b),
				(float) Math.Max(a, b));
			if (!EditorGUI.EndChangeCheck())
				return;
			ValueEntry.SmartValue = GetValue(value);
		}

		private float GetValue(float value)
		{
			var minAttribute = ValueEntry.Property.GetAttribute<MinValueAttribute>();
			var maxAttribute = ValueEntry.Property.GetAttribute<MaxValueAttribute>();
			var rangeAttribute = ValueEntry.Property.GetAttribute<PropertyRangeAttribute>();
			var unityRangeAttribute = ValueEntry.Property.GetAttribute<RangeAttribute>();

			if (unityRangeAttribute != null)
			{
				if (value < unityRangeAttribute.min)
					return unityRangeAttribute.min;
				if (value > unityRangeAttribute.max)
					return unityRangeAttribute.max;
			}

			if (rangeAttribute != null)
			{
				var minValue = Resolve(Property, rangeAttribute.MinGetter, rangeAttribute.Min);
				if (value < minValue)
					return minValue;

				var maxValue = Resolve(Property, rangeAttribute.MaxGetter, rangeAttribute.Max);
				if (value > maxValue)
					return minValue;
			}

			if (minAttribute != null)
			{
				var minValue = Resolve(Property, minAttribute.Expression, minAttribute.MinValue);
				if (value < minValue)
					return minValue;
			}

			if (maxAttribute != null)
			{
				var maxValue = Resolve(Property, maxAttribute.Expression, maxAttribute.MaxValue);
				if (value > maxValue)
					return maxValue;
			}

			return value;

			float Resolve(InspectorProperty property, string expression, double defaultValue)
			{
				return (float) ValueResolver.Get(property, expression, defaultValue).GetValue();
			}
		}
	}
}
