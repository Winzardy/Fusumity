using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class EvaluatedValueDrawer : OdinValueDrawer<IEvaluatedValue>
	{
		private GUIContent _tooltip;

		protected override void Initialize()
		{
			_tooltip = new GUIContent(string.Empty, "Использовать Evaluator");
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			SirenixEditorGUI.BeginIndentedHorizontal();
			CallNextDrawer(label);

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (ValueEntry.SmartValue.Evaluator == null)
			{
				EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(15));
				var lastRect = GUILayoutUtility.GetLastRect();
				if (FusumityEditorGUILayout.SuffixSDFButton(lastRect, SdfIconType.ArrowRight, _tooltip, width: 10, offset: 4.5f,
					    useHover: true))
				{
					var boxedValue = ValueEntry.SmartValue;
					boxedValue.ToEvaluatorMode();
					ValueEntry.WeakSmartValue = boxedValue;

					Property.Parent.MarkSerializationRootDirty();
					return;
				}
			}

			if (ValueEntry.SmartValue.Evaluator is IConstantEvaluator)
			{
				var boxedValue = ValueEntry.SmartValue;
				boxedValue.ToConstantMode();
				ValueEntry.WeakSmartValue = boxedValue;
				Property.Parent.MarkSerializationRootDirty();
				return;
			}

			SirenixEditorGUI.EndIndentedHorizontal();
		}
	}
}
