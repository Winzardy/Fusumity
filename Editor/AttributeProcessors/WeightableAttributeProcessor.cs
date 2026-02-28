using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Fusumity.Attributes;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public abstract class BaseWeightableAttributeProcessor<T> : OdinAttributeProcessor<T>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "weight":
					attributes.Add(new DarkCardBoxAttribute());

					attributes.Add(new SuffixLabelAttribute("@WeightableAttributeProcessor.GetSuffixLabel($property)", true));
					break;
			}
		}
	}

	public class WeightableAttributeProcessor : BaseWeightableAttributeProcessor<IWeightable>
	{
		private const string SPACE = "     ";

		public static string GetSuffixLabel(InspectorProperty property)
		{
			if (property.ParentValueProperty.ParentValueProperty.ValueEntry.WeakSmartValue is ICollection collection)
			{
				var totalWeight = 0;
				var currentWeight = property.ParentValueProperty.ValueEntry.WeakSmartValue is IWeightable current ? current.Weight : 0;
				foreach (var obj in collection)
				{
					if (obj is IWeightable weightable)
					{
						totalWeight += weightable.Weight;
					}
				}

				var percent = (float) currentWeight / totalWeight;
				return percent.ToString("P4") + SPACE;
			}

			return "?%" + SPACE;
		}
	}

	public class WeightableWithEvaluatorAttributeProcessor : BaseWeightableAttributeProcessor<IWeightableWithEvaluator>
	{
	}
}
