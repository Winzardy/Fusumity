using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ProxyEvaluatorValueDrawer : OdinValueDrawer<IProxyEvaluator>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIContent.none);
			{
				var width = 14;
				var useOffset = EditorGUIUtility.hierarchyMode && ValueEntry.SmartValue.Proxy != null;
				if (useOffset)
					width += 11;
				EditorGUILayout.LabelField(string.Empty, GUILayout.Width(width));
				var iconRect = GUILayoutUtility.GetLastRect();

				var guiContent = new GUIContent(string.Empty);
				guiContent.tooltip = "Cбросить";
				iconRect.x -= 2;
				iconRect.width -= useOffset ? 10 : 0;
				if (GUI.Button(iconRect, guiContent, GUIStyle.none))
				{
					ValueEntry.WeakSmartValue = null;
				}

				var isHover = iconRect.Contains(Event.current.mousePosition);
				iconRect.x += 2;
				iconRect.width += useOffset ? 10 : 0;

				iconRect.y += 0.3f;
				iconRect.x += 4.5f;
				iconRect.width = 10f;

				var origin = GUI.color;

				GUI.color = isHover ? origin : origin * 0.8f;
				SdfIcons.DrawIcon(iconRect, SdfIconType.ArrowLeft);
				GUI.color = origin;

				CallNextDrawer(label);
			}
			SirenixEditorGUI.EndHorizontalPropertyLayout();
		}
	}
}
