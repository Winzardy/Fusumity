#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Trading.Editor
{
	public class TradeCostAttributeProcessor : TradeCostAttributeProcessor<TradeCost>
	{
		public static bool Filter(Type type, InspectorProperty property)
		{
			if (type.GetCustomAttribute<SerializableAttribute>(inherit: false) == null)
				return false;

			var fieldAccess = TradeAccessType.Low;
			var fieldInfo = property?.Info?.GetMemberInfo() as FieldInfo;
			if (fieldInfo != null)
			{
				var fieldAttr = fieldInfo.GetCustomAttribute<TradeAccessAttribute>();
				if (fieldAttr != null)
					fieldAccess = fieldAttr.Access;
			}

			var typeAttr = type.GetCustomAttribute<TradeAccessAttribute>();
			var typeAccess = typeAttr?.Access ?? TradeAccessType.Low;
			return fieldAccess >= typeAccess;
		}
	}

	public abstract class TradeCostAttributeProcessor<T> : ShowMonoScriptForReferenceAttributeProcessor<T>
		where T : TradeCost
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
			{
				FilterTypesFunction = GetFilterFunction(),
			};

			attributes.Add(typeSelectorSettingsAttribute);
		}

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));
		}

		protected virtual string GetFilterFunction()
		{
			return $"@{nameof(TradeCostAttributeProcessor)}.{nameof(TradeCostAttributeProcessor.Filter)}($type, $property)";
		}
	}
}
