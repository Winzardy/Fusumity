using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Localization.Editor
{
	public class LocKeyAttributeProcessor : OdinAttributeProcessor<LocKey>
	{
		private static readonly Dictionary<InspectorProperty, GUIContent> _propertyToGUIContent = new();

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var guiContent = new GUIContent(property.Label);
			_propertyToGUIContent[property] = guiContent;
			if (attributes.GetAttribute<LabelTextAttribute>() != null)
				guiContent.text = attributes.GetAttribute<LabelTextAttribute>().Text;
			if (attributes.GetAttribute<HideLabelAttribute>() != null)
				guiContent.text = string.Empty;
			if (attributes.GetAttribute<TooltipAttribute>() != null)
				guiContent.tooltip = attributes.GetAttribute<TooltipAttribute>().tooltip;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name == nameof(LocKey.value))
			{
				if (_propertyToGUIContent.TryGetValue(parentProperty, out var content))
				{
					if (!content.text.IsNullOrEmpty())
						attributes.Add(new LabelTextAttribute(content.text));
					else
						attributes.Add(new HideLabelAttribute());

					if (!content.tooltip.IsNullOrEmpty())
						attributes.Add(new TooltipAttribute(content.tooltip));

					if (parentProperty.GetAttribute<CanBeNullAttribute>() != null)
						attributes.Add(new CanBeNullAttribute());
				}
				else
					attributes.Add(new HideLabelAttribute());
			}
		}
	}
}
