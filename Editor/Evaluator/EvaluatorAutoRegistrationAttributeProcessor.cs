using System;
using System.Collections.Generic;
using Sapientia;
using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class EvaluatorAutoRegistrationAttributeProcessor : OdinAttributeProcessor<IEvaluator>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var valueEntryTypeOfValue = property.ValueEntry.TypeOfValue;
			if (!valueEntryTypeOfValue.IsGenericType)
				return;

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!

			var genericTypeDefinition = valueEntryTypeOfValue.GetGenericTypeDefinition();

			if (genericTypeDefinition == typeof(IfElseEvaluator<,>))
			{
				EvaluatorTypeRegistryUtility.Register(
					valueEntryTypeOfValue,
					"\u2009If / else",
					"/",
					new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A),
					SdfIconType.Alt,
					1);
			}
		}
	}
}
