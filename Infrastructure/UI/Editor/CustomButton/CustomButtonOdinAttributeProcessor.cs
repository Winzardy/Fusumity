using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
	public class CustomButtonOdinAttributeProcessor : OdinAttributeProcessor<CustomButton>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "m_Script":
				case "m_Interactable":
				case "m_TargetGraphic":
				case "m_Transition":
				case "m_Colors":
				case "m_SpriteState":
				case "m_AnimationTriggers":
				case "m_Navigation":
				case "m_OnClick":
					attributes.Add(new HideInInspector());
					break;

				case nameof(CustomButton.transitions):
					var attribute = new ListDrawerSettingsAttribute
					{
						OnTitleBarGUI = $"@{nameof(CustomButtonOdinAttributeProcessor)}.{nameof(DrawButton)}($property)"
					};
					attributes.Add(attribute);
					break;
			}
		}

		private static void DrawButton(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not CustomButton button)
				return;

			if (!SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
				return;

			button.Refresh();
#if UNITY_EDITOR
			EditorUtility.SetDirty(button);
#endif
		}
	}
}
