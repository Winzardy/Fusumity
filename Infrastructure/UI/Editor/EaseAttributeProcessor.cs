using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class EaseAttributeProcessor : OdinAttributeProcessor<Ease>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new InlineButtonAttribute("@EaseAttributeProcessor.Show()",
				SdfIconType.GraphUp, string.Empty));
		}

		public static void Show()
		{
			Application.OpenURL("https://easings.net/");
		}
	}
}
