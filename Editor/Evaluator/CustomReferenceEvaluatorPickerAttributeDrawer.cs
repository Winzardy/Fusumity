using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class CustomReferenceEvaluatorPickerAttributeDrawer : OdinAttributeDrawer<CustomReferenceEvaluatorPickerAttribute>
	{
		private GUIContent _tooltip;

		protected override void Initialize()
		{
			_tooltip = new GUIContent(string.Empty, "Использовать Evaluator");
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			GUILayout.BeginHorizontal();
			{
				CallNextDrawer(label);
				EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(15));
				var lastRect = GUILayoutUtility.GetLastRect();

				if (FusumityEditorGUILayout.SuffixSDFButton(lastRect, SdfIconType.ArrowRight, _tooltip, width: 10, offset: 4.5f,
					useHover: true))
				{
					Property.Parent.ValueEntry.WeakSmartValue = null;
					Property.Parent.MarkSerializationRootDirty();
					return;
				}
			}
			GUILayout.EndHorizontal();
		}
	}
}
