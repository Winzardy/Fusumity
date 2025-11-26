using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.Editor
{
	public class ContentSerializeReferenceAttributeProcessor : OdinAttributeProcessor<IContentSerializeReference>
	{
		private static readonly string LABEL_GUID = "Guid";

		public static readonly string TOOLTIP_PREFIX_GUID = $"{LABEL_GUID}:\n".ColorText(Color.gray).SizeText(12);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case ContentConstants.VALUE_FIELD_NAME:

					if (ContentEntryAttributeProcessor.propertyToGUIContent.TryGetValue(parentProperty.Parent, out var label))
					{
						if (label.text != null)
							attributes.Add(new LabelTextAttribute(label.text));

						if (label.tooltip != null)
							attributes.Add(new TooltipAttribute(label.tooltip));

						if (!parentProperty.Parent.Attributes.HasAttribute<DisableContentEntryDrawerAttribute>())
							attributes.Add(new TooltipAttribute(
								$"@{nameof(ContentSerializeReferenceAttributeProcessor)}.{nameof(GetTooltip)}($property, \"{label.tooltip}\")"));
					}
					else
						attributes.Add(new HideLabelAttribute());

					ContextMenu();
					break;
			}

			void ContextMenu()
			{
				attributes.Add(new CustomContextMenuAttribute(
					"Guid/Copy",
					$"@{nameof(ContentSerializeReferenceAttributeProcessor)}.{nameof(CopyGuid)}($property)"));
				attributes.Add(new CustomContextMenuAttribute(
					"Guid/Regenerate",
					$"@{nameof(ContentSerializeReferenceAttributeProcessor)}.{nameof(RegenerateGuid)}($property)"));
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
			attributes.Add(new HideReferenceObjectPickerAttribute());
		}

		public static void CopyGuid(InspectorProperty property)
		{
			if (property.Parent.Parent.ValueEntry.WeakSmartValue is not IUniqueContentEntry contentEntry)
				return;

			Clipboard.Copy(contentEntry.Guid.ToString());
		}

		public static void RegenerateGuid(InspectorProperty property)
		{
			if (property.Parent.Parent.ValueEntry.WeakSmartValue is not IUniqueContentEntry contentEntry)
				return;

			ContentEditorCache.RegenerateGuid(contentEntry,
				property.Parent.Parent.UnityPropertyPath,
				property.Tree.UnitySerializedObject.targetObject);
		}

		public static string GetTooltip(InspectorProperty property, string tooltip)
		{
			if (property.Parent?.Parent?.ValueEntry?.WeakSmartValue is IUniqueContentEntry contentEntry)
			{
				if (!tooltip.IsNullOrEmpty())
					tooltip += ContentReferenceConstants.TOOLTIP_SPACE;

				tooltip += $"{TOOLTIP_PREFIX_GUID}{contentEntry.Guid}";
			}

			return tooltip;
		}
	}
}
