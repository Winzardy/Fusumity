using System;
using System.Collections.Generic;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Booting.Editor
{
	public class BootTaskAttributeProcessor : ShowMonoScriptForReferenceAttributeProcessor<IBootTask>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			if (property.ValueEntry.WeakSmartValue is IBootTask task)
			{
				if (!task.Active)
					attributes.Add(new GUIColorAttribute(0.8f, 0.8f, 0.8f, 0.2f));
			}
		}
	}
}
