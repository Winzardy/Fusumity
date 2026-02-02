using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using HideIfAttribute = Sirenix.OdinInspector.HideIfAttribute;
using ShowIfAttribute = Sirenix.OdinInspector.ShowIfAttribute;

namespace UI.Editor
{
	public class UIBaseLayoutAttributeProcessor : OdinAttributeProcessor<UIBaseLayout>
	{
		private static readonly string[] CONTROL_CHILD_NAMES =
		{
			"useAnimations",
			"useAnimation",
			"useLayoutAnimations"
		};

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new InlineEditorAttribute(InlineEditorObjectFieldModes.Foldout));
		}

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var controlAnimations = IsControlAnimations(parentProperty, out var controlAnimationsName);

			if (member.Name == controlAnimationsName)
				attributes.Add(new ToggleGroupAttribute(controlAnimationsName, "Layout Animations"));

			switch (member.Name)
			{
				case nameof(UIBaseLayout.rectTransform):

					attributes.Add(new PropertySpaceAttribute(0, 8));
					attributes.Add(new ReadOnlyAttribute());
					attributes.Add(new PropertyOrderAttribute(-1));
					break;

				case nameof(UIBaseLayout.prefab):
					attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.prefab), null));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new PropertyOrderAttribute(-2));
					attributes.Add(new ReadOnlyAttribute());

					break;

				case nameof(UIBaseLayout.openingSequence):

					attributes.Add(new LabelTextAttribute("Opening"));
					if (controlAnimations)
					{
						attributes.Add(new BoxGroupAttribute(controlAnimationsName + "/VisibilityO", false));
					}
					else
					{
						attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.UseLayoutAnimations)));
						attributes.Add(new FoldoutGroupAttribute("Layout Animations"));
						attributes.Add(new BoxGroupAttribute("Layout Animations/VisibilityO", false));
					}

					break;

				case nameof(UIBaseLayout.closingSequence):

					attributes.Add(new LabelTextAttribute("Closing"));
					if (controlAnimations)
					{
						attributes.Add(new BoxGroupAttribute(controlAnimationsName + "/VisibilityС", false));
					}
					else
					{
						attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.UseLayoutAnimations)));
						attributes.Add(new BoxGroupAttribute("Layout Animations/VisibilityС", false));
					}

					break;

				case nameof(UIBaseLayout.customSequences):
					attributes.Add(new LabelTextAttribute("Sequences"));
					attributes.Add(new TableListAttribute());
					if (controlAnimations)
					{
						attributes.Add(new HorizontalGroupAttribute(controlAnimationsName + "/Custom"));
					}
					else
					{
						attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.UseLayoutAnimations)));
						attributes.Add(new HorizontalGroupAttribute("Layout Animations/Custom"));
					}

					break;
				case nameof(UIBaseLayout.debugCurrentKey):
					attributes.Add(new HideIfAttribute(nameof(UIBaseLayout.HideDebugAnimationInEditor)));
					attributes.Add(new PropertySpaceAttribute(10));
					attributes.Add(new PropertyOrderAttribute(99));
					attributes.Add(new ValueDropdownAttribute(nameof(UIBaseLayout.debugAnimationKeys)));
					attributes.Add(new LabelTextAttribute("Debug Animation"));
					attributes.Add(new InlineButtonAttribute(nameof(UIBaseLayout.PlayAnimation), SdfIconType.Play,
						"Play"));
					break;

				case nameof(UIBaseLayout.debugAnimationKeys):
					attributes.Add(new HideInInspector());
					break;

				#region Container

				case nameof(UIBaseContainerLayout.container):
					attributes.Add(new PropertySpaceAttribute(0, 8));
					break;

				case nameof(UIBaseContainerLayout.openingBlendMode):
					attributes.Add(new LabelTextAttribute("Blend Mode"));
					if (controlAnimations)
					{
						attributes.Add(new BoxGroupAttribute(controlAnimationsName + "/VisibilityO", false));
					}
					else
					{
						attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.UseLayoutAnimations)));
						attributes.Add(new BoxGroupAttribute("Layout Animations/VisibilityO", false));
					}

					break;
				case nameof(UIBaseContainerLayout.closingBlendMode):
					attributes.Add(new LabelTextAttribute("Blend Mode"));
					if (controlAnimations)
					{
						attributes.Add(new BoxGroupAttribute(controlAnimationsName + "/VisibilityС", false));
					}
					else
					{
						attributes.Add(new ShowIfAttribute(nameof(UIBaseLayout.UseLayoutAnimations)));
						attributes.Add(new BoxGroupAttribute("Layout Animations/VisibilityС", false));
					}

					break;

				#endregion

				case nameof(UICanvasLayout.canvas):
					attributes.Add(new ReadOnlyAttribute());
					break;
			}
		}

		private bool IsControlAnimations(InspectorProperty parentProperty, out string name)
		{
			name = null;

			foreach (var x in CONTROL_CHILD_NAMES)
			{
				name = x;
				var fieldInfo = parentProperty.ValueEntry.WeakSmartValue.GetType().GetField(x);
				if (fieldInfo != null)
					return true;

				name = "_" + x;
				fieldInfo = parentProperty.ValueEntry.WeakSmartValue.GetType().GetField(name,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (fieldInfo != null)
					return true;
			}

			return false;
		}
	}
}
