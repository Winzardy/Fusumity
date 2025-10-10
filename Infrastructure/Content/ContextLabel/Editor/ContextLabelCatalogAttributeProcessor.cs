using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Content.ContextLabel.Editor
{
	public class ContextLabelCatalogAttributeProcessor : OdinAttributeProcessor<IContextLabelCatalog>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "_keyToLabel":
					var settings = new DictionaryDrawerSettings
					{
						KeyLabel = "Key",
						ValueLabel = "Label"
					};
					attributes.Add(settings);
					break;
			}
		}
	}

	public class ContextLabelAttributeProcessor : OdinAttributeProcessor<IToggle>
	{
		private static Dictionary<InspectorProperty, string> _propertyToCatalog = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "value":
					if (_propertyToCatalog.TryGetValue(parentProperty, out var catalog))
						attributes.Add(new ContextLabelAttribute(catalog));

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			var attribute = property.GetAttribute<ContextLabelAttribute>();

			if (attribute != null)
			{
				_propertyToCatalog[property] = attribute.Catalog;
				attributes.Remove(attribute);
			}
		}
	}
}
