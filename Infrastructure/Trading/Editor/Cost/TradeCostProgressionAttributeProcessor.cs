#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeCostProgressionAttributeProcessor : TradeCostAttributeProcessor<TradeCostProgression>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TradeCostProgression.stages):
					var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
					{
						FilterTypesFunction = $"@{nameof(TradeCostProgressionAttributeProcessor)}.{nameof(Filter)}($type, $property)"
					};

					attributes.Add(typeSelectorSettingsAttribute);

					break;

				case nameof(TradeCostProgression.condition):
					attributes.Add(new PropertySpaceAttribute(1, 5));
					attributes.Add(new LabelTextAttribute("Increment Condition"));
					break;
			}

			if (!IsInsideCollection(parentProperty))
				attributes.Add(new IndentAttribute(-1));
		}

		public static bool Filter(Type type, InspectorProperty property)
		{
			if (type == typeof(TradeCostOptions)) // Не обрабатываем кейс с вложенным выбором...
				return false;

			if (!TradeCostAttributeProcessor.Filter(type, property.Parent))
				return false;

			return type.HasAttribute<SerializableAttribute>();
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

	public class TradeCostProgressionStageAttributeProcessor : OdinAttributeProcessor<TradeCostProgressionStage>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(TradeCostProgressionStage.useOverrideCondition):
					attributes.Add(new LabelTextAttribute("Use Override Increment Condition"));
					break;

				case nameof(TradeCostProgressionStage.overrideCondition):
					attributes.Add(new ShowIfAttribute(nameof(TradeRewardProgressionStage.useOverrideCondition)));
					attributes.Add(new LabelTextAttribute("Increment Condition"));
					break;
			}
		}
	}
}
