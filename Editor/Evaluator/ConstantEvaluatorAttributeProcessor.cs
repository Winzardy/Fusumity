using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConstantEvaluatorAttributeProcessor : ValueWrapperOdinAttributeProcessor<IConstantEvaluator>
	{
		protected override string ValueFieldName => "value";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			if (member.Name == ValueFieldName)
			{
				attributes.Add(new CustomReferenceEvaluatorPickerAttribute());
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideReferenceObjectPickerAttribute());

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!
			var typeConfig = TypeRegistryUserConfig.Instance;
			var constantType = property.ValueEntry.TypeOfValue;
			var settings = typeConfig.TryGetSettings(constantType);
			if (settings == null)
			{
				settings = new TypeSettings();
				typeConfig.SetSettings(constantType, settings);
				EditorUtility.SetDirty(typeConfig);
			}

			settings.Name = "\u2009Constant";
			settings.Category = "/";
			settings.DarkIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			settings.LightIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			settings.Icon = SdfIconType.DiamondFill;
		}
	}

	public class CustomReferenceEvaluatorPickerAttribute : Attribute
	{
	}
}
