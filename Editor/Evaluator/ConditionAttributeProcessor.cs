using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Conditions;
using Sapientia.Evaluators;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConditionCustomDrawerAttribute : Attribute
	{
	}

	public class ConditionAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<ICondition>
	{
		public static SdfIconType NoneConditionSdfIcon { get => SdfIconType.Check; }
		public static string NoneConditionLabel { get => "\u2009None (true)"; }
		public static SdfIconType RejectConditionSdfIcon { get => SdfIconType.X; }
		public static string RejectConditionLabel { get => "\u2009Reject (false)"; }
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

			if (!typeof(IProxyEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
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
			var typeConfig = TypeRegistryUserConfig.Instance;
			var settings = typeConfig.TryGetSettings(valueEntryTypeOfValue);

			if (genericTypeDefinition == typeof(IfElseCondition<>))
			{
				if (settings == null)
				{
					settings = new TypeSettings();
					typeConfig.SetSettings(valueEntryTypeOfValue, settings);
					EditorUtility.SetDirty(typeConfig);
				}

				settings.Name = "\u2009If / else";
				settings.Category = "/";
				settings.DarkIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.LightIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.Icon = SdfIconType.Alt;

				typeConfig.SetPriority(valueEntryTypeOfValue, 1, null);
			}

			if (genericTypeDefinition == typeof(NoneCondition<>))
			{
				if (settings == null)
				{
					settings = new TypeSettings();
					typeConfig.SetSettings(valueEntryTypeOfValue, settings);
					EditorUtility.SetDirty(typeConfig);
				}

				settings.Name = NoneConditionLabel;
				settings.Category = "/";
				settings.DarkIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.LightIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.Icon = NoneConditionSdfIcon;

				typeConfig.SetPriority(valueEntryTypeOfValue, 10001, null);
			}

			if (genericTypeDefinition == typeof(RejectCondition<>))
			{
				if (settings == null)
				{
					settings = new TypeSettings();
					typeConfig.SetSettings(valueEntryTypeOfValue, settings);
					EditorUtility.SetDirty(typeConfig);
				}

				settings.Name = RejectConditionLabel;
				settings.Category = "/";
				settings.DarkIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.LightIconColor = new Color(ICondition.R, ICondition.G, ICondition.B, ICondition.A);
				settings.Icon = RejectConditionSdfIcon;

				typeConfig.SetPriority(valueEntryTypeOfValue, 10000, null);
			}
		}
	}
}
