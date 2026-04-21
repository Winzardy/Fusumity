using System;
using System.Collections.Generic;
using Fusumity.Attributes;
using Fusumity.Attributes.Odin;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;
using UnityEngine.EventSystems;

namespace UI.Editor
{
	public class UIBehaviourAttributeProcessor : OdinAttributeProcessor<UIBehaviour>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			if (property.ValueEntry.WeakSmartValue != null &&
				(property.Parent == null || !property.Parent.AnyParentHasAttribute<FoldoutContainerAttribute>()) &&
				!property.ValueEntry.TypeOfValue.IsSubclassOf(typeof(TMP_Text)) &&
				!property.AnyParentHasAttribute<InlineToggleAttribute>())
				attributes.Add(new InlineEditorAttribute(InlineEditorObjectFieldModes.Foldout));

			base.ProcessSelfAttributes(property, attributes);
		}
	}
}
