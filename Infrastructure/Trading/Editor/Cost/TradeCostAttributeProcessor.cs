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
			var fieldInfo = property.Info?.GetMemberInfo() as FieldInfo;
			if (fieldInfo != null)
			{
				var fieldAttr = fieldInfo.GetCustomAttribute<TradeAccessAttribute>();
				if (fieldAttr != null)
				{
					if (fieldAttr.Access == TradeAccessType.ByParent)
					{
						fieldAccess = ResolveSource(property.Parent, out _, out _);
					}
					else
					{
						fieldAccess = fieldAttr.Access;
					}
				}
			}

			var typeAttr = type.GetCustomAttribute<TradeAccessAttribute>();
			var typeAccess = typeAttr?.Access ?? TradeAccessType.Low;
			return fieldAccess >= typeAccess;
		}

		public static TradeAccessType ResolveSource(
			InspectorProperty property,
			out InspectorProperty sourceProperty,
			out FieldInfo sourceField)
		{
			sourceProperty = null;
			sourceField = null;

			for (var p = property; p != null; p = p.Parent)
			{
				if (p.Info?.GetMemberInfo() is not FieldInfo fi)
					continue;

				var attr = fi.GetCustomAttribute<TradeAccessAttribute>(inherit: true);
				if (attr == null)
					continue;

				if (attr.Access != TradeAccessType.ByParent)
				{
					sourceProperty = p;
					sourceField = fi;
					return attr.Access;
				}
			}

			return default;
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
