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

		private string _tooltip;
		private GUIContent _label;

		protected override void Initialize()
		{
			_tooltip = $"Прокси на другой тип:\n<u>{ValueEntry.SmartValue.ProxyType.GetNiceName()}</u>\n\n" +
				$"Нажмите, чтобы вернуть:\n<u>{Property.Info.TypeOfValue.GetNiceName()}</u>";
			_label = new GUIContent(string.Empty)
			{
				tooltip = _tooltip
			};
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var smartValueProxy = ValueEntry.SmartValue.Proxy;
			var useOffset = EditorGUIUtility.hierarchyMode && smartValueProxy != null;

			if (Property.LastDrawnValueRect.x != 0 && Property.LastDrawnValueRect.y != 20) // Хак, лучше не придумал
				_iconRect = Property.LastDrawnValueRect.AlignLeft(15).AlignTop(EditorGUIUtility.singleLineHeight);

			if (_iconRect.HasValue)
			{
				var iconRect = _iconRect.Value;
				var b = smartValueProxy != null || !EditorGUIUtility.hierarchyMode;
				var isZeroIndent = EditorGUI.indentLevel == 0;
				var b1 = b && isZeroIndent;
				if (b1)
					EditorGUI.indentLevel++;

				var hierarchyMode = EditorGUIUtility.hierarchyMode && isZeroIndent;
				if (hierarchyMode)
				{
					iconRect.x -= 14;
				}
				else if (useOffset)
				{
					iconRect.x -= 11;
				}

				if (!hierarchyMode)
					iconRect.x += GUIHelper.CurrentIndentAmount - iconRect.width;

				iconRect.x -= 1.5f;
				iconRect.width -= useOffset ? 3 : 0;
				if (GUI.Button(iconRect, _label, GUIStyle.none))
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
