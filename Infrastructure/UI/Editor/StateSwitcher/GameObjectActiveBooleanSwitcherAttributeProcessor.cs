using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace UI.Editor
{
	public class GameObjectActiveBooleanSwitcherAttributeProcessor : OdinAttributeProcessor<GameObjectActiveBooleanSwitcher>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case "_default":
				case "_dictionary":
					var switcher = parentProperty.ValueEntry.WeakSmartValue as GameObjectActiveBooleanSwitcher;
					if (switcher != null && switcher.HasCustomActiveMapping)
						break;

					attributes.Add(new HideInInspector());
					break;
			}
		}
	}
}
