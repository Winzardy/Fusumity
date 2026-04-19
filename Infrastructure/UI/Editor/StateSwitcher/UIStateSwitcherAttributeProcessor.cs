using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Editor
{
	public class UIStateSwitcherAttributeProcessor : OdinAttributeProcessor<IStateSwitcher>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new InlineEditorAttribute(InlineEditorObjectFieldModes.Foldout));

			if(property.Parent != null)
				return;
			if (property.ValueEntry.WeakSmartValue is IStateSwitcher stateSwitcher)
			{
				var type = stateSwitcher.StateType;
				if (type != typeof(bool)
					&& type != typeof(int)
					&& type != typeof(string))
					attributes.Add(new InfoBoxAttribute($"Настоятельно рекомендуется использовать типы: <b>bool</b>, <b>int</b>, <b>string</b>, вместо типа: <b><u>{type.Name}</u></b>",
						InfoMessageType.Warning));
			}
		}
	}
}
