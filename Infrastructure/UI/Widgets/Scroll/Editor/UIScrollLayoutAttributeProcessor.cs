using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace UI.Scroll.Editor
{
	public class UIScrollLayoutAttributeProcessor : OdinAttributeProcessor<UIScrollLayout>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(UIScrollLayout.template):
					attributes.Add(new ShowIfAttribute($"@{nameof(UIScrollLayoutAttributeProcessor)}." +
						$"{nameof(ShowIfPreserveTemplate)}($property)"));
					break;
				case "_" + nameof(UIScrollLayout.scrollRect):
					attributes.Add(new ReadOnlyAttribute());
					attributes.Add(new PropertySpaceAttribute(0, 8));
					break;

				case nameof(UIScrollLayout.NormalizedScrollPosition):
				case nameof(UIScrollLayout.ScrollPosition):
				case nameof(UIScrollLayout.LinearVelocity):
					attributes.Add(new HideInEditorModeAttribute());
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ReadOnlyAttribute());
					break;

				case nameof(UIScrollLayout.snapVelocityThreshold):
				case nameof(UIScrollLayout.snapWatchOffset):
				case nameof(UIScrollLayout.snapUseItemSpacing):
				case nameof(UIScrollLayout.snapTweenType):
				case nameof(UIScrollLayout.snapTweenTime):
				case nameof(UIScrollLayout.snapWhileDragging):
				case nameof(UIScrollLayout.forceSnapOnEndDrag):
					attributes.Add(new ShowIfAttribute(nameof(UIScrollLayout.snapping)));
					break;

				case nameof(UIScrollLayout.useScrollSequence):
					attributes.Add(new PropertySpaceAttribute(8, 0));
					attributes.Add(new ToggleGroupAttribute(nameof(UIScrollLayout.useScrollSequence), groupTitle:"Scroll Sequence"));
					break;
				case nameof(UIScrollLayout.scrollSequence):
					attributes.Add(new ToggleGroupAttribute(nameof(UIScrollLayout.useScrollSequence)));
					break;
			}
		}

		public static bool ShowIfPreserveTemplate(InspectorProperty property)
		{
			if (property.Parent.ValueEntry.WeakSmartValue is UIScrollLayout layout)
			{
				return layout.preserveTemplate;
			}

			return false;
		}
	}
}
