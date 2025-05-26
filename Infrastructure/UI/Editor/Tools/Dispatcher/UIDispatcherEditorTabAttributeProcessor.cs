using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Editor
{
	public class UIDispatcherEditorTabAttributeProcessor : OdinAttributeProcessor<IUIDispatcherEditorTab>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new DisableInEditorModeAttribute());
		}
	}
}
