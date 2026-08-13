using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class SchedulePointAttributeProcessor : OdinAttributeProcessor<ISchedulePoint>
	{
		private const string INTERVAL_WARNING_MESSAGE =
			"Нет смысла использовать больше двух интервалов! Потому что они конфликтуют друг с другом";

		private const string INTERVAL_DURATION_MESSAGE =
			"У Interval окна не бывает: он задаёт моменты от точки отсчёта. " +
			"Смени тип точки или убери длительность";

		private const string OVERLAP_WARNING_MESSAGE =
			"Окно [ {0} ] длиннее периода повторения [ {1} ] — вхождения будут накладываться друг на друга";

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
			{
				// Длительность лежит не в точке, а рядом — в durations схемы, по тому же индексу
				if (property.Parent?.Parent?.ValueEntry?.WeakSmartValue is ScheduleScheme scheme &&
					TryGetDurationMessage(schedulePoint, scheme.GetWindowDuration(property.Index), out message))
					return true;

				if (property.Parent.Parent.ValueEntry.WeakSmartValue is ScheduleScheme {points: {Length: >= 2}} schedule)
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
			}

			return false;
		}

		/// <summary>
		/// Диагностика окна: у Interval окон нет вовсе, а окно длиннее периода повторения
		/// накладывается само на себя
		/// </summary>
		private static bool TryGetDurationMessage(SchedulePoint point, long duration, out string message)
		{
			message = null;

			if (duration <= 0)
				return false;

			var kind = point.GetKind();

			if (kind is SchedulePointKind.Interval)
			{
				message = INTERVAL_DURATION_MESSAGE;
				return true;
			}

			var period = ScheduleUtility.GetMinPeriodSeconds(kind);

			if (duration <= period)
				return false;

			message = string.Format(OVERLAP_WARNING_MESSAGE,
				TimeSpan.FromSeconds(duration).ToLabel(true, false),
				TimeSpan.FromSeconds(period).ToLabel(true, false));

			return true;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SchedulePointCodeAttribute : Attribute
	{
	}
}
