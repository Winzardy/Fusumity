using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sapientia;
using Sapientia.Extensions;
using Sapientia.Utility;
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

		protected virtual string EmptyLabel { get => null; }

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			if (member.Name == ValueFieldName || member.Name == SecondValueFieldName)
			{
				if (propertyToGUIContent.TryGetValue(parentProperty, out var content))
				{
					if (!content.text.IsNullOrEmpty())
						attributes.Add(new LabelTextAttribute(content.text));
					else
					{
						if (EmptyLabel.IsNullOrEmpty())
							attributes.Add(new HideLabelAttribute());
						else
							attributes.Add(new LabelTextAttribute(EmptyLabel));
					}

					if (!content.tooltip.IsNullOrEmpty())
						attributes.Add(new TooltipAttribute(content.tooltip));

					if (SecondValueFieldName != string.Empty)
					{
						if (member.Name == SecondValueFieldName)
							propertyToGUIContent.Remove(parentProperty);
					}
					else
					{
						propertyToGUIContent.Remove(parentProperty);
					}
				}
				else
				{
					if (EmptyLabel.IsNullOrEmpty())
						attributes.Add(new HideLabelAttribute());
					else
						attributes.Add(new LabelTextAttribute(EmptyLabel));
				}
			}

			if (member.Name == ValueFieldName)
			{
				foreach (var parentAttribute in parentProperty.Attributes)
				{
					if (parentAttribute is IAttributeConvertible attribute)
					{
						attributes.Add(typeof(IContainer).IsAssignableFrom(member.DeclaringType)
							? parentAttribute
							: attribute.Convert());
					}
				}
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
			else if (property.Parent?.ChildResolver is IOrderedCollectionResolver)
				guiContent.text = string.Empty;

			if (attributes.GetAttribute<TooltipAttribute>() != null)
				guiContent.tooltip = attributes.GetAttribute<TooltipAttribute>().tooltip;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}
	}
}
