using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public sealed class LabeledPropertyRangeAttributeFloatDrawer : BaseLabeledPropertyRangeAttributeDrawer<float>
	{
		protected override void DrawSlider(GUIContent label)
		{
			var a = this.getterMinValue?.GetValue() ?? (float) this.Attribute.Min;
			var b = this.getterMaxValue?.GetValue() ?? (float) this.Attribute.Max;
			if (this.getterMinValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(this.getterMinValue.ErrorMessage);
			if (this.getterMaxValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(this.getterMaxValue.ErrorMessage);
			EditorGUI.BeginChangeCheck();
			float num = SirenixEditorFields.RangeFloatField(label, this.ValueEntry.SmartValue, Mathf.Min(a, b), Mathf.Max(a, b));
			if (!EditorGUI.EndChangeCheck())
				return;
			this.ValueEntry.SmartValue = num;
		}
	}
}
