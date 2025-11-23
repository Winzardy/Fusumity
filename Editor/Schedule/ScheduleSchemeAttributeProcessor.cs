using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ScheduleSchemeAttributeProcessor : OdinAttributeProcessor<ScheduleScheme>
	{
		private const string IGNORE_LABEL = "value";
		private const string DEFAULT_LABEL = "Schedule";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(ScheduleScheme.points):

					var parentLabelContent = parentProperty.Label;

					if (!parentLabelContent.text.IsNullOrEmpty() && parentLabelContent.text.ToLowerInvariant() != IGNORE_LABEL)
						attributes.Add(new LabelTextAttribute(parentLabelContent.text));
					else
						attributes.Add(new LabelTextAttribute(DEFAULT_LABEL));

					if (!parentLabelContent.tooltip.IsNullOrEmpty())
						attributes.Add(new TooltipAttribute(parentLabelContent.tooltip));
					else if (parentProperty.Info.GetMemberInfo().TryGetSummary(out summary))
						attributes.Add(new TooltipAttribute(summary));

					// var expr = "@ScheduleSchemeAttributeProcessor.ValidatePoints($property)";
					// attributes.Add(new ValidateInputAttribute(expr, "Can't be empty or null"));

					break;
			}
		}

		public static bool ValidatePoints(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is SchedulePoint[] points)
			{
				return points.Length > 0;
			}

			return false;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideLabelAttribute());
		}
	}
}
