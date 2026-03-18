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

				if (currentWeight <= 0)
					return string.Empty;

				foreach (var obj in collection)
				{
					if (obj is IWeightable weightable)
					{
						totalWeight += weightable.Weight;
					}
				}

				if (totalWeight == 0)
					totalWeight = 1;

				var percent = (float) currentWeight / totalWeight;
				return percent.ToString("0.####%") + SPACE;
			}

			return "?%" + SPACE;
		}
	}

	public class WeightableWithEvaluatorAttributeProcessor : BaseWeightableAttributeProcessor<IWeightableWithEvaluator>
	{
	}
}
