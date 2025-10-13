using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ProxyEvaluatorValueDrawer : OdinValueDrawer<IProxyEvaluator>
	{
		private Rect? _iconRect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var smartValueProxy = ValueEntry.SmartValue.Proxy;
			var useOffset = EditorGUIUtility.hierarchyMode && smartValueProxy != null;

			if (Property.LastDrawnValueRect.x != 0 && Property.LastDrawnValueRect.y != 20) // Хак, лучше не придумал
				_iconRect = Property.LastDrawnValueRect.AlignLeft(15).AlignTop(18);

			if (_iconRect.HasValue)
			{
				var iconRect = _iconRect.Value;
				var b = smartValueProxy != null || !EditorGUIUtility.hierarchyMode;
				var b1 = b && EditorGUI.indentLevel == 0;
				if (b1)
					EditorGUI.indentLevel++;

				var guiContent = new GUIContent(string.Empty);

				guiContent.tooltip = $"Прокси на другой тип:\n<u>{ValueEntry.SmartValue.ProxyType.GetNiceName()}</u>\n\n" +
					$"Нажмите, чтобы вернуть:\n<u>{Property.Info.TypeOfValue.GetNiceName()}</u>";

				var b2 = EditorGUI.indentLevel == (b1 ? 1 : 0);
				if (EditorGUIUtility.hierarchyMode && b2)
				{
					iconRect.x -= 11;
					iconRect = iconRect.AlignTop(23);
				}
				else if (useOffset)
				{
					iconRect.x -= 11;
				}

				iconRect.x -= 1.5f;
				iconRect.width -= useOffset ? 3 : 0;
				if (GUI.Button(iconRect, guiContent, GUIStyle.none))
				{
					ValueEntry.WeakSmartValue = null;
				}

				var isHover = iconRect.Contains(Event.current.mousePosition);
				iconRect.width += useOffset ? 3 : 0;

				iconRect.y += 0.3f;
				iconRect.x += 4.5f;
				iconRect.width = 10f;

				var origin = GUI.color;

				GUI.color = isHover ? origin : origin * 0.8f;
				SdfIcons.DrawIcon(iconRect, SdfIconType.ArrowLeft);
				GUI.color = origin;
				CallNextDrawer(label);
				if (b1)
					EditorGUI.indentLevel--;
			}
			else
			{
				CallNextDrawer(label);
			}
		}
	}
}
