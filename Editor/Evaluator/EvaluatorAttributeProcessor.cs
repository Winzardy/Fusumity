using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Evaluators;
using Sapientia.Extensions.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class EvaluatorAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<IEvaluator>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			if (typeof(IConstantEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return;

			TypeSelectorSettingsAttribute typeSelectorSettingsAttribute;
			if (TopSemanticAncestorIsCondition(property))
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction = $"@{nameof(EvaluatorAttributeProcessor)}.{nameof(FilterByConditionRoot)}($type, $property)",
					ShowNoneItem = false,
				};

				attributes.Add(typeSelectorSettingsAttribute);
			}
			else
			{
				typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction = $"@{nameof(EvaluatorAttributeProcessor)}.{nameof(Filter)}($type, $property)",
					ShowNoneItem = false,
				};
			}

			attributes.Add(typeSelectorSettingsAttribute);

			var c = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			var color = Color.Lerp(c, Color.white, 0.83f);
			if (attributes.GetAttribute<GUIColorAttribute>() != null)
				return;
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));

			if (typeof(IProxyEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return;

			color = Color.Lerp(c, Color.black, 0.83f);
			if (attributes.GetAttribute<ColorCardBoxAttribute>() != null)
				return;
			attributes.Add(new ColorCardBoxAttribute(
				color.r,
				color.g,
				color.b,
				0.2f));
		}

		// Тут фильтр для случая когда корнем древа был Condition
		private static bool FilterByConditionRoot(Type type, InspectorProperty property)
		{
			if (typeof(IRandomEvaluator).IsAssignableFrom(type))
				return false;

			return Filter(type, property);
		}

		private static bool Filter(Type type, InspectorProperty property)
		{
			if (typeof(IConstantEvaluator).IsAssignableFrom(type))
			{
				if (type.IsGenericType)
				{
					var valueType = type.GetGenericArguments()
						.SecondOrDefault();

					if (valueType != null)
					{
						if (!valueType.IsUnitySerializableType())
							return false;
					}
				}
			}

			return true;
		}

		private static bool TopSemanticAncestorIsCondition(InspectorProperty property)
		{
			InspectorProperty top = null;

			for (var p = property; p != null; p = p.Parent)
			{
				var t = p.ValueEntry?.BaseValueType;
				if (t == null)
					continue;

				if (typeof(IEvaluator).IsAssignableFrom(t) || typeof(ICondition).IsAssignableFrom(t))
					top = p;
			}

			var topType = top?.ValueEntry?.BaseValueType;
			return topType != null && typeof(ICondition).IsAssignableFrom(topType);
		}
	}
}
