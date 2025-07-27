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
	public class ResetScheduleAttributeProcessor : OdinAttributeProcessor<ResetSchedule>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.TryGetSummary(out var summary))
				attributes.Add(new TooltipAttribute(summary));

			switch (member.Name)
			{
				case nameof(ResetSchedule.points):

					var parentLabelContent = parentProperty.Label;

					if (!parentLabelContent.text.IsNullOrEmpty())
						attributes.Add(new LabelTextAttribute(parentLabelContent.text));
					else
						attributes.Add(new HideLabelAttribute());

					if (!parentLabelContent.tooltip.IsNullOrEmpty())
						attributes.Add(new TooltipAttribute(parentLabelContent.tooltip));
					else if (parentProperty.Info.GetMemberInfo().TryGetSummary(out summary))
						attributes.Add(new TooltipAttribute(summary));

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideLabelAttribute());
		}
	}
}
