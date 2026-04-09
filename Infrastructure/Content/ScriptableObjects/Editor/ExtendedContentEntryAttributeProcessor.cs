using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Content.ScriptableObjects.Editor
{
	public class ExtendedContentEntryAttributeProcessor : OdinAttributeProcessor<IContentEntry>
	{
		public override bool CanProcessSelfAttributes(InspectorProperty property)
		{
			var type = property.ValueEntry.TypeOfValue;
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ContentEntry<>);
		}

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (parentProperty.ValueEntry.WeakSmartValue is not IContentEntry contentEntry)
				return;

			switch (member.Name)
			{
				case ContentConstants.VALUE_FIELD_NAME:
					attributes.Add(new CustomContextMenuAttribute(
						"Guid/Regenerate All (Recursive)",
						$"@{nameof(ExtendedContentEntryAttributeProcessor)}.{nameof(RegenerateGuidAll)}($property)"));

					break;
			}
		}

		public static void RegenerateGuidAll(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is not IUniqueContentEntry contentEntry)
				return;

			var root = property.SerializationRoot.ValueEntry.WeakSmartValue as ContentScriptableObject;
			if (root == null)
				return;

			property.Parent.RegenerateGuid(contentEntry, root);
		}
	}
}
