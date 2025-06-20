using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class GraphicGroupAttributeProcessor : OdinAttributeProcessor<GraphicGroup>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member,
			List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "m_Color":
				case "m_Material":
					attributes.Add(new HideInInspector());
					break;

				case nameof(GraphicGroup.autoCollectGraphicsOnRuntime):
					attributes.Add(new PropertyOrderAttribute(-1));
					break;

				case nameof(GraphicGroup.graphics):
					var listDrawerSettingsAttribute = new ListDrawerSettingsAttribute
					{
						OnTitleBarGUI = nameof(GraphicGroup.DrawAddButtonEditor)
					};
					attributes.Add(listDrawerSettingsAttribute);
					attributes.Add(new PropertyOrderAttribute(-1));
					attributes.Add(new PropertySpaceAttribute(5, 5));
					attributes.Add(new DisableIfAttribute(nameof(GraphicGroup.autoCollectGraphicsOnRuntime)));
					break;

				case "m_RaycastPadding":
					attributes.Add(new EnableIfAttribute("m_RaycastTarget"));
					attributes.Add(new RectOffsetAttribute());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new HideMonoScriptAttribute());
		}
	}
}
