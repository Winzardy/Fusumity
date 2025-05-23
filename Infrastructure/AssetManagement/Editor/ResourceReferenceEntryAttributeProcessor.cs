using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;

namespace AssetManagement.Editor
{
	public class ResourceReferenceEntryAttributeProcessor : BaseAssetReferenceEntryAttributeProcessor<IResourceReferenceEntry>
	{
		protected override string FieldName => "_path";
		protected override bool NeedHandleAssetReferenceT(InspectorProperty property) => false;

		protected override void OnProcessFieldAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			if (member.ReflectedType != null)
			{
				attributes.Add(new ResourceSelectorAttribute(
					member.ReflectedType.GetGenericArguments()[0]));
			}
		}
	}
}
