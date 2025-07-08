using System;
using System.Collections.Generic;
using System.Reflection;
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
}
