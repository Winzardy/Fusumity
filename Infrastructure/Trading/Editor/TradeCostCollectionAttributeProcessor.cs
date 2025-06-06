#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Trading.Editor
{
	public class TradeCostCollectionAttributeProcessor : OdinAttributeProcessor<TradeCostCollection>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeCostCollection.items):
					var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
					{
						FilterTypesFunction = nameof(TradeCostCollection.Filter)
					};
					attributes.Add(typeSelectorSettingsAttribute);
					if (!IsInsideCollection(parentProperty))
						attributes.Add(new IndentAttribute(-1));
					break;
			}
		}

		private bool IsInsideCollection(InspectorProperty? property)
		{
			while (property != null)
			{
				if (property.Parent?.ChildResolver is IOrderedCollectionResolver)
					return true;

				property = property.Parent;
			}

			return false;
		}
	}

}
