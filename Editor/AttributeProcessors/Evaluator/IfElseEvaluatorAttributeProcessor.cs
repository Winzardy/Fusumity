using System;
using System.Collections.Generic;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class IfElseEvaluatorAttributeProcessor : OdinAttributeProcessor<IEvaluator>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var valueEntryTypeOfValue = property.ValueEntry.TypeOfValue;
			if (valueEntryTypeOfValue.IsGenericType &&
			    valueEntryTypeOfValue.GetGenericTypeDefinition() == typeof(IfElseEvaluator<,>))
			{
				// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
				// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!
				var typeConfig = TypeRegistryUserConfig.Instance;
				var constantType = valueEntryTypeOfValue;
				typeConfig.SetSettings(constantType, new TypeSettings
				{
					Name = "\u2009If / else",
					Category = "/",
					DarkIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A),
					LightIconColor = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A),
					Icon = SdfIconType.Alt,
				});
				EditorUtility.SetDirty(typeConfig);
			}
		}
	}
}
