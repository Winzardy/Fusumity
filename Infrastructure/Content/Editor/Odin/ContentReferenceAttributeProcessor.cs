using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.Editor
{
	public class ContentReferenceAttributeProcessor : OdinAttributeProcessor<IContentReference>
	{
		public static readonly Dictionary<InspectorProperty, GUIContent> propertyToGUIContent = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case IContentReference.GUID_FIELD_NAME:

					if (propertyToGUIContent.TryGetValue(parentProperty, out var content))
					{
						if (!content.text.IsNullOrEmpty())
							attributes.Add(new LabelTextAttribute(content.text));
						else
							attributes.Add(new HideLabelAttribute());

						if (!content.tooltip.IsNullOrEmpty())
							attributes.Add(new TooltipAttribute(content.tooltip));
					}
					else
						attributes.Add(new HideLabelAttribute());

					//...
					if (member.ReflectedType != null)
						attributes.Add(new ContentReferenceAttribute(
							member.ReflectedType.GetGenericArguments()[0]));

					break;

				case IContentReference.INDEX_FIELD_NAME:
					attributes.Add(new HideInInspector());
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var guiContent = new GUIContent(property.Label);
			propertyToGUIContent[property] = guiContent;

			if (attributes.GetAttribute<HideLabelAttribute>() != null)
				guiContent.text = string.Empty;
			else if (attributes.GetAttribute<LabelTextAttribute>() != null)
				guiContent.text = attributes.GetAttribute<LabelTextAttribute>().Text;

			if (attributes.GetAttribute<TooltipAttribute>() != null)
				guiContent.tooltip = attributes.GetAttribute<TooltipAttribute>().tooltip;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}
	}
}
