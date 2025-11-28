using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Evaluators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace Fusumity.Editor
{
	public class ProxyEvaluatorAttributeProcessor : ValueWrapperOdinAttributeProcessor<IProxyEvaluator>
	{
		protected override string ValueFieldName => "value";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name == ValueFieldName)
			{
				attributes.Add(new CustomContextMenuAttribute("Proxy/Clear",
					$"@{nameof(ProxyEvaluatorAttributeProcessor)}.{nameof(OnContextMenuClearClicked)}($property)"));
				attributes.Add(new CustomContextMenuAttribute("Proxy/Copy",
					$"@{nameof(ProxyEvaluatorAttributeProcessor)}.{nameof(OnContextMenuCopyClicked)}($property)"));
				attributes.Add(new CustomContextMenuAttribute("Proxy/Paste",
					$"@{nameof(ProxyEvaluatorAttributeProcessor)}.{nameof(OnContextMenuPasteClicked)}($property)"));
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideReferenceObjectPickerAttribute());
		}

		public static void OnContextMenuClearClicked(InspectorProperty property)
		{
			property.Parent.ValueEntry.WeakSmartValue = null;
		}

		public static void OnContextMenuCopyClicked(InspectorProperty property)
		{
			Clipboard.Copy(property.Parent.ValueEntry.WeakSmartValue);
		}

		public static void OnContextMenuPasteClicked(InspectorProperty property)
		{
			if (Clipboard.CanPaste(property.ParentType))
				property.Parent.ValueEntry.WeakSmartValue = Clipboard.Paste();
		}
	}
}
