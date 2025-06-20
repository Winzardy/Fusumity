using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public sealed class LabeledPropertyRangeAttributeIntDrawer : BaseLabeledPropertyRangeAttributeDrawer<int>
	{
		protected override void DrawSlider(GUIContent label)
		{
			int a = this.getterMinValue?.GetValue() ?? (int) this.Attribute.Min;
			int b = this.getterMaxValue?.GetValue() ?? (int) this.Attribute.Max;
			if (this.getterMinValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(this.getterMinValue.ErrorMessage);
			if (this.getterMaxValue is {ErrorMessage: not null})
				SirenixEditorGUI.ErrorMessageBox(this.getterMaxValue.ErrorMessage);
			EditorGUI.BeginChangeCheck();
			int num = SirenixEditorFields.RangeIntField(label, ValueEntry.SmartValue, (int) Mathf.Min((float) a, (float) b),
				(int) Mathf.Max((float) a, (float) b));
			if (!EditorGUI.EndChangeCheck())
				return;
			if (num < 0)
				num = 0;
			else
				this.ValueEntry.SmartValue = num;
			this.ValueEntry.SmartValue = num;
		}
	}
}
