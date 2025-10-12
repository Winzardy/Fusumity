using System;
using System.Collections.Generic;
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
	public class ActionAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<IAction>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var c = new Color(IAction.R, IAction.G, IAction.B, IAction.A);
			var color = Color.Lerp(c, Color.white, 0.83f);
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));

			if (typeof(IProxyEvaluator).IsAssignableFrom(property.ValueEntry.TypeOfValue))
				return;

			color = Color.Lerp(c, Color.black, 0.83f);
			attributes.Add(new ColorCardBoxAttribute(
				color.r,
				color.g,
				color.b,
				0.2f));
		}
	}
}
