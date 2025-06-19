using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Sapientia;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

namespace Fusumity.Editor
{
	public class ToggleAttributeProcessor : OdinAttributeProcessor<IToggle>
	{
		private const string ENABLE_FIELD_NAME = "enable";
		private const string VALUE_FIELD_NAME = "value";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case ENABLE_FIELD_NAME:
					//TODO: нужно как-то узнать что треугольник отрисовывается впереди
					var offset = 14; // 14 + 11 (отступ для dropdown)
					if (parentProperty.GetAttribute<ToggleOffsetAttribute>() != null)
						offset += parentProperty.GetAttribute<ToggleOffsetAttribute>().Offset;
					attributes.Add(new HorizontalGroupAttribute(nameof(ToggleAttributeProcessor), offset));
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new PropertySpaceAttribute(1.5f));
					break;

				case VALUE_FIELD_NAME:
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new HorizontalGroupAttribute(nameof(ToggleAttributeProcessor)));
					attributes.Add(new EnableIfAttribute(ENABLE_FIELD_NAME));

					var type = member.GetReturnType();
					var isHolder = typeof(IContainer).IsAssignableFrom(type);

					foreach (var parentAttribute in parentProperty.Attributes)
						if (parentAttribute is ParentAttribute attribute)
							attributes.Add(isHolder ? attribute : attribute.Convert());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			attributes.Add(new InlinePropertyAttribute());
			attributes.Add(new PropertySpaceAttribute(-1.5f, -1));

			base.ProcessSelfAttributes(property, attributes);
		}
	}
}
