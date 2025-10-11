using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Evaluator;
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

			if (TopSemanticAncestorIsCondition(property))
			{
				var typeSelectorSettingsAttribute = new TypeSelectorSettingsAttribute
				{
					FilterTypesFunction = $"@{nameof(EvaluatorAttributeProcessor)}.{nameof(FilterByConditionRoot)}($type, $property)"
				};

				attributes.Add(typeSelectorSettingsAttribute);
			}

			var c = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			var color = Color.Lerp(c, Color.white, 0.83f);
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));

			color = Color.Lerp(c, Color.black, 0.83f);
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

				if (typeof(IEvaluator).IsAssignableFrom(t) || typeof(IBlackboardCondition).IsAssignableFrom(t))
					top = p;
			}

			var topType = top?.ValueEntry?.BaseValueType;
			return topType != null && typeof(IBlackboardCondition).IsAssignableFrom(topType);
		}
	}
}
