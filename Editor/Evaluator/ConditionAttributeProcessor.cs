using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Conditions;
using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConditionCustomDrawerAttribute : Attribute
	{
	}

	public class ConditionAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<ICondition>
	{
		public const string NONE_CONDITION_LABEL = "\u2009None (true)";
		public const SdfIconType NONE_CONDITION_SDF_ICON = SdfIconType.Check;

		public const string REJECT_CONDITION_LABEL = "\u2009Reject (false)";
		public const SdfIconType REJECT_CONDITION_SDF_ICON = SdfIconType.X;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "invert":
					attributes.Add(new HorizontalGroupAttribute(ICondition.GROUP, 23, marginRight: 2));
					attributes.Add(new LabelTextAttribute("!"));
					attributes.Add(new LabelWidthAttribute(8));
					attributes.Add(new TooltipAttribute("Инвертировать результат"));
					break;

				case "mode":
					attributes.Add(new HorizontalGroupAttribute(ICondition.GROUP));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			// Такое хак делается для отрисовки полиморфных полей, так как у OdinValueDrawer не работает отрисовка если объект null!
			attributes.Add(new ConditionCustomDrawerAttribute());

			var color = Color.Lerp(Color.blue, Color.white, 0.83f);

			if (attributes.FirstOrDefault(a => a is GUIColorAttribute) is GUIColorAttribute colorAttribute)
			{
				colorAttribute.Color = color;
			}
			else
			{
				attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));
			}

			if (!typeof(IBridgeEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
			{
				color = Color.Lerp(Color.blue, Color.black, 0.83f);
				if (attributes.FirstOrDefault(a => a is ColorCardBoxAttribute) is ColorCardBoxAttribute box)
				{
					box.R = color.r;
					box.G = color.g;
					box.B = color.b;
					box.A = 0.2f;
				}
				else
				{
					attributes.Add(new ColorCardBoxAttribute(
						color.r,
						color.g,
						color.b,
						0.2f));
				}
			}

			var valueEntryTypeOfValue = property.ValueEntry.TypeOfValue;
			if (!valueEntryTypeOfValue.IsGenericType)
				return;

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!
			var genericTypeDefinition = valueEntryTypeOfValue.GetGenericTypeDefinition();

			if (genericTypeDefinition == typeof(IfElseCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					valueEntryTypeOfValue,
					"\u2009If / else",
					"/",
					new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A),
					SdfIconType.Alt,
					1);
			}

			if (genericTypeDefinition == typeof(NoneCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					valueEntryTypeOfValue,
					NONE_CONDITION_LABEL,
					"/",
					new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A),
					NONE_CONDITION_SDF_ICON,
					10001);
			}

			if (genericTypeDefinition == typeof(RejectCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					valueEntryTypeOfValue,
					REJECT_CONDITION_LABEL,
					"/",
					new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A),
					REJECT_CONDITION_SDF_ICON,
					10000);
			}
		}
	}
}
