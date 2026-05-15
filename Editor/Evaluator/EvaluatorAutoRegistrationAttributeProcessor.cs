using System;
using System.Collections.Generic;
using Sapientia;
using Sapientia.Conditions;
using Sapientia.Conditions.Comparison;
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

			var type = property.ValueEntry.TypeOfValue;
			if (!type.IsGenericType)
				return;

			// Хак для того чтобы в селекторе был <> Constant, проблема в том что над Generic
			// TypeRegisterItemAttribute не работает, точнее работает, просто его важен конченый тип, а Generic не определенный тип!

			var genericTypeDefinition = type.GetGenericTypeDefinition();

			#region Condition

			if (genericTypeDefinition == typeof(IfElseCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					"\u2009If / else",
					"/",
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					SdfIconType.Alt,
					1);
			}

			if (genericTypeDefinition == typeof(NoneCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.NONE_CONDITION_LABEL,
					"/",
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.NONE_CONDITION_SDF_ICON,
					10001);
			}

			if (genericTypeDefinition == typeof(RejectCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.REJECT_CONDITION_LABEL,
					"/",
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.REJECT_CONDITION_SDF_ICON,
					10000);
			}

			if (genericTypeDefinition == typeof(CollectionCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.COLLECTION_CONDITION_LABEL,
					"/",
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.COLLECTION_CONDITION_SDF_ICON,
					100);
			}

			if (genericTypeDefinition == typeof(IntCompareCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.INT_COMPARE_NAME,
					EvaluatorTypeRegistryConstants.INT_COMPARE_CATEGORY,
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.INT_COMPARE_ICON,
					100);
			}

			if (genericTypeDefinition == typeof(IntInRangeCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.INT_RANGE_CONDITION_NAME,
					EvaluatorTypeRegistryConstants.INT_RANGE_CONDITION_CATEGORY,
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.INT_RANGE_CONDITION_ICON,
					EvaluatorTypeRegistryConstants.INT_RANGE_CONDITION_PRIORITY);
			}

			if (genericTypeDefinition == typeof(Fix64InRangeCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_NAME,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_CATEGORY,
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_ICON,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_PRIORITY);
			}

			if (genericTypeDefinition == typeof(Fix64InRangeCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_NAME,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_CATEGORY,
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_ICON,
					EvaluatorTypeRegistryConstants.FIX64_RANGE_CONDITION_PRIORITY);
			}

			if (genericTypeDefinition == typeof(Fix64CompareCondition<>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					EvaluatorTypeRegistryConstants.FIX64_COMPARE_NAME,
					EvaluatorTypeRegistryConstants.FIX64_COMPARE_CATEGORY,
					EvaluatorTypeRegistryConstants.CONDITION_COLOR,
					EvaluatorTypeRegistryConstants.FIX64_COMPARE_ICON,
					1);
			}

			#endregion

			#region Evaluators

			if (genericTypeDefinition == typeof(IfElseEvaluator<,>))
			{
				EvaluatorTypeRegistryUtility.Register(
					type,
					"\u2009If / else",
					"/",
					EvaluatorTypeRegistryConstants.EVALUATOR_COLOR,
					SdfIconType.Alt,
					1);
			}

			#endregion
		}
	}
}
