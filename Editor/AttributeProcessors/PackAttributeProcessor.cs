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
	public class PackAttributeProcessor : OdinAttributeProcessor<IPack>
	{
		/// <summary>
		/// <see cref="Pack{T}.target"/>
		/// </summary>
		public const string TARGET_FIELD_NAME = "target";

		/// <summary>
		/// <see cref="Pack{T}.amount"/>
		/// </summary>
		public const string AMOUNT_FIELD_NAME = "amount";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var type = member.GetReturnType();
			var isHolder = typeof(IHolder).IsAssignableFrom(type);

			switch (member.Name)
			{
				case TARGET_FIELD_NAME:
					attributes.Add(new HorizontalGroupAttribute());
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new SuffixLabelAttribute("\u2715"));
					break;
				case AMOUNT_FIELD_NAME:
					attributes.Add(new HorizontalGroupAttribute(width: 0.3f));
					attributes.Add(new HideLabelAttribute());
					foreach (var parentAttribute in parentProperty.Attributes)
						if (parentAttribute is ParentAttribute attribute)
							attributes.Add(isHolder ? attribute : attribute.Convert());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new InlinePropertyAttribute
			{
				LabelWidth = 28
			});
		}
	}
}
