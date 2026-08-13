using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Content.Keys.Editor
{
	public class KeyCatalogAttributeProcessor : OdinAttributeProcessor<IKeyCatalog>
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

	public class KeyAttributeProcessor : OdinAttributeProcessor<IToggle>
	{
		private static Dictionary<InspectorProperty, string> _propertyToCatalog = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "value":
					if (_propertyToCatalog.TryGetValue(parentProperty, out var catalog))
						attributes.Add(new KeyAttribute(catalog));

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			var attribute = property.GetAttribute<KeyAttribute>();

			if (attribute != null)
			{
				_propertyToCatalog[property] = attribute.CatalogId;
				attributes.Remove(attribute);
			}
		}
	}
}
