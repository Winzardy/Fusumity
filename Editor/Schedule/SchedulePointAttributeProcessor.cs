using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class SchedulePointAttributeProcessor : OdinAttributeProcessor<ISchedulePoint>
	{
		private const string INTERVAL_WARNING_MESSAGE =
			"Нет смысла использовать больше двух интервалов! Потому что они конфликтуют друг с другом";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case "type":
					if (parentProperty.Parent.ChildResolver is not ICollectionResolver)
					{
						var parentLabelContent = parentProperty.Label;
						if (!parentLabelContent.text.IsNullOrEmpty())
							attributes.Add(new LabelTextAttribute(parentLabelContent.text));

						if (!parentLabelContent.tooltip.IsNullOrEmpty())
						{
							var exp3 = $"@{nameof(SchedulePointAttributeProcessor)}.{nameof(GetTooltip)}($property)";
							attributes.Add(new PropertyTooltipAttribute(exp3));
						}
					}

					break;

				case "code":
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new SchedulePointCodeAttribute());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			var messageExp = $"@{nameof(SchedulePointAttributeProcessor)}.{nameof(GetWarningMessage)}($property)";
			var warningVisibleExp = $"@{nameof(SchedulePointAttributeProcessor)}.{nameof(VisibleWarningMessage)}($property)";
			attributes.Add(new InfoBoxAttribute(messageExp, InfoMessageType.Error, warningVisibleExp));
			var color = Color.black.WithAlpha(0.4f);
			attributes.Add(new ColorCardBoxAttribute(color.r, color.g, color.b, color.a));

			attributes.Add(new HideLabelAttribute());
		}

		public static string GetTooltip(InspectorProperty property)
		{
			var parentLabelContent = property.Parent.Label;
			if (!parentLabelContent.tooltip.IsNullOrEmpty())
				return parentLabelContent.tooltip + "\n\n" + property.Label.tooltip;
			return property.Label.tooltip;
		}

		public static string GetWarningMessage(InspectorProperty property)
			=> TryGetWarningMessage(property, out var message) ? message : string.Empty;

		public static bool VisibleWarningMessage(InspectorProperty property)
			=> TryGetWarningMessage(property, out _);

		private static bool TryGetWarningMessage(InspectorProperty property, out string message)
		{
			message = null;
			if (property.ValueEntry.WeakSmartValue is SchedulePoint schedulePoint)
				if (property.Parent.Parent.ValueEntry.WeakSmartValue is ScheduleEntry {points: {Length: >= 2}} schedule)
				{
					if (schedulePoint.GetKind() is SchedulePointKind.Interval)
					{
						var count = 0;
						for (var i = 0; i < schedule.points.Length; i++)
						{
							if (schedule.points[i].GetKind() is not SchedulePointKind.Interval)
								continue;

							count++;

							if (count < 2)
								continue;

							message = INTERVAL_WARNING_MESSAGE;
							return true;
						}
					}
				}

			return false;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SchedulePointCodeAttribute : Attribute
	{
	}
}
