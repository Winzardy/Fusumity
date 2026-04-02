#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Trading.Editor
{
	public class TradeCostOptionsAttributeProcessor : TradeCostAttributeProcessor<TradeCostOptions>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeCostCollection.items):
					attributes.Add(GetTypeSelectorAttribute());

					if (!IsInsideCollection(parentProperty))
						attributes.Add(new IndentAttribute(-1));
					break;
			}
		}

		protected override string GetFilterFunction()
		{
			return $"@{nameof(TradeCostOptionsAttributeProcessor)}.{nameof(Filter)}($type, $property)";
		}

		public static bool Filter(Type type, InspectorProperty property)
		{
			if (type == typeof(TradeCostOptions)) // Не обрабатываем кейс с вложенным выбором...
				return false;

			if (!TradeCostAttributeProcessor.Filter(type, property.Parent))
				return false;

			return !typeof(IEnumerable<TradeCost>)
			   .IsAssignableFrom(type) && type.HasAttribute<SerializableAttribute>();
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
