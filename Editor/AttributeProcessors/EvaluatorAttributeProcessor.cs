using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Fusumity.Editor.Utility;
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
			var c = new Color(IEvaluator.R, IEvaluator.G, IEvaluator.B, IEvaluator.A);
			var color = Color.Lerp(c, Color.white, 0.83f);
			attributes.Add(new GUIColorAttribute(color.r, color.g, color.b));
			attributes.Add(new ColorCardBoxAttribute(
				Color.black.r,
				Color.black.g,
				Color.black.b,
				0.2f));
		}
	}
}
