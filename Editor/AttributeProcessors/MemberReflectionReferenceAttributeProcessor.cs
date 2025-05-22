using System;
using System.Collections.Generic;
using System.Reflection;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Generic.Editor
{
	public class MemberReflectionReferenceAttributeProcessor : OdinAttributeProcessor<IMemberReflectionReference>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case IMemberReflectionReference.STEPS_FIELD_NAME:
					attributes.Add(new HideInInspector());
					break;
				case IMemberReflectionReference.PATH_FIELD_NAME:
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new PropertyOrderAttribute(-1));
					attributes.Add(new HideLabelAttribute());
					break;
				case IMemberReflectionReference.CACHE_FIELD_NAME:
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute(IMemberReflectionReference.CACHE_FIELD_NAME, null));
					attributes.Add(new ReadOnlyAttribute());
					break;
			}
		}
	}

	public class MemberReferencePathStepAttributeProcessor : OdinAttributeProcessor<MemberReferencePathStep>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(MemberReferencePathStep.name):
					attributes.Add(new ReadOnlyAttribute());
					break;
				case nameof(MemberReferencePathStep.index):
					attributes.Add(new ReadOnlyAttribute());
					attributes.Add(new ShowIfAttribute(nameof(MemberReferencePathStep.IsArrayElement)));
					break;
			}
		}
	}
}
