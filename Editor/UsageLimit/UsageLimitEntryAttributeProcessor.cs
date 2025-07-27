using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Fusumity.Attributes.Specific;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	using ShowIfAttribute = Sirenix.OdinInspector.ShowIfAttribute;

	public class UsageLimitEntryAttributeProcessor : OdinAttributeProcessor<UsageLimitEntry>
	{
		private const string USAGE_COUNT_SUFFIX = "usage";
		private const string USAGE_COUNT_UNLIMITED_SUFFIX = "unlimited usages";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(UsageLimitEntry.usageCount):

					attributes.Add(new MinimumAttribute(0));
					var parentLabelContent = parentProperty.Label;
					var exp = $"@{nameof(UsageLimitEntryAttributeProcessor)}.{nameof(GetUsageCountSuffixValue)}($value)";
					attributes.Add(new SuffixValueAttribute(exp, parentLabelContent));
					if (!parentLabelContent.text.IsNullOrEmpty())
						attributes.Add(new LabelTextAttribute(parentLabelContent.text));
					else
						attributes.Add(new HideLabelAttribute());

					if (!parentLabelContent.tooltip.IsNullOrEmpty())
					{
						var exp3 = $"@{nameof(UsageLimitEntryAttributeProcessor)}.{nameof(GetUsageCountTooltip)}($property)";
						attributes.Add(new PropertyTooltipAttribute(exp3));
					}

					break;
				case nameof(UsageLimitEntry.reset):
					var exp1 = $"@{nameof(UsageLimitEntryAttributeProcessor)}.{nameof(ShowIfUsageCooldown)}($property)";
					attributes.Add(new ShowIfAttribute(exp1));
					attributes.Add(new PropertySpaceAttribute(0, 5));
					break;
				case nameof(UsageLimitEntry.fullReset):

					var exp2 = $"@{nameof(UsageLimitEntryAttributeProcessor)}.{nameof(ShowIfCooldown)}($property)";
					attributes.Add(new ShowIfAttribute(exp2));
					break;
			}
		}

		public static string GetUsageCountTooltip(InspectorProperty property)
		{
			var parentLabelContent = property.Parent.Label;
			if (!parentLabelContent.tooltip.IsNullOrEmpty())
				return parentLabelContent.tooltip + "\n\n" + property.Label.tooltip;
			return property.Label.tooltip;
		}

		public static string GetUsageCountSuffixValue(int usageCount)
		{
			if (usageCount == 0)
				return USAGE_COUNT_UNLIMITED_SUFFIX;

			return usageCount > 1 ? $"{USAGE_COUNT_SUFFIX}s" : USAGE_COUNT_SUFFIX;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideLabelAttribute());
			var color = Color.Lerp(Color.Lerp(Color.red, Color.yellow, 0.5f), Color.Lerp(Color.gray, Color.black, 0.5f), 0.5f)
			   .WithAlpha(0.3f);
			attributes.Add(new ColorCardBoxAttribute(color.r, color.g, color.b, color.a));
		}

		public static bool ShowIfCooldown(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is UsageLimitEntry entry)
				return entry.usageCount > 0;

			return false;
		}

		public static bool ShowIfUsageCooldown(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is UsageLimitEntry entry)
				return entry.usageCount > 1;

			return false;
		}
	}
}
