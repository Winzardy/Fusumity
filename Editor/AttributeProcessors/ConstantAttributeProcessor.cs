using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Evaluator;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class ConstantAttributeProcessor : OdinAttributeProcessor<IConstantEvaluator>
	{
		public static readonly Dictionary<InspectorProperty, GUIContent> propertyToGUIContent = new();

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);
			switch (member.Name)
			{
				case "value":
					attributes.Add(new CustomReferencePickerAttribute());

					if (propertyToGUIContent.TryGetValue(parentProperty, out var content))
					{
						if (!content.text.IsNullOrEmpty())
							attributes.Add(new LabelTextAttribute(content.text));
						else
							attributes.Add(new HideLabelAttribute());

						if (!content.tooltip.IsNullOrEmpty())
							attributes.Add(new TooltipAttribute(content.tooltip));
					}
					else
						attributes.Add(new HideLabelAttribute());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new HideReferenceObjectPickerAttribute());

			var guiContent = new GUIContent(property.Label);
			propertyToGUIContent[property] = guiContent;

			if (attributes.GetAttribute<HideLabelAttribute>() != null)
				guiContent.text = string.Empty;
			else if (attributes.GetAttribute<LabelTextAttribute>() != null)
				guiContent.text = attributes.GetAttribute<LabelTextAttribute>().Text;

			if (attributes.GetAttribute<TooltipAttribute>() != null)
				guiContent.tooltip = attributes.GetAttribute<TooltipAttribute>().tooltip;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}
	}

	public class CustomReferencePickerAttribute : Attribute
	{
	}

	public class CustomDrawerAttribute : OdinAttributeDrawer<CustomReferencePickerAttribute>
	{
		private static readonly Rect ONE = new(0, 0, 1, 1);
		private Rect? _rect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (FusumityEditorGUILayout.SuffixSDFButton(_rect, Draw, SdfIconType.ArrowRight))
			{
				Property.Parent.ValueEntry.WeakSmartValue = null;
				Property.Parent.MarkSerializationRootDirty();
				return;
			}

			var lastRect = GUILayoutUtility.GetLastRect();
			if (lastRect != ONE)
				_rect = lastRect;

			void Draw()
			{
				CallNextDrawer(label);
			}
		}
	}
}
