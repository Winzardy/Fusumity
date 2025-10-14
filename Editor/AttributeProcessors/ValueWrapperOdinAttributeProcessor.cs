using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public abstract class ValueWrapperOdinAttributeProcessor<TValue> : OdinAttributeProcessor<TValue>
	{
		public static readonly Dictionary<InspectorProperty, GUIContent> propertyToGUIContent = new();

		protected abstract string ValueFieldName { get; }
		protected virtual string SecondValueFieldName => string.Empty;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name == ValueFieldName || member.Name == SecondValueFieldName)
			{
				if (propertyToGUIContent.TryGetValue(parentProperty, out var content))
				{
					if (!content.text.IsNullOrEmpty())
						attributes.Add(new LabelTextAttribute(content.text));
					else
						attributes.Add(new HideLabelAttribute());

					if (!content.tooltip.IsNullOrEmpty())
						attributes.Add(new TooltipAttribute(content.tooltip));

					propertyToGUIContent.Remove(parentProperty);
				}
				else
					attributes.Add(new HideLabelAttribute());
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
