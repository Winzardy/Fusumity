using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audio.Player;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Audio.Editor
{
	public class AudioSettingsAttributeProcessor : OdinAttributeProcessor<AudioSettings>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(AudioSettings.disableAudioPlayers):
					attributes.Add(new SpaceAttribute());
					var valuesGetter = $"@{nameof(AudioSettingsAttributeProcessor)}.{nameof(GetAllAudioPlayers)}()";
					var dropdown = new ValueDropdownAttribute(valuesGetter)
					{
						IsUniqueList = true
					};
					attributes.Add(dropdown);
					break;
			}
		}

		public static IEnumerable GetAllAudioPlayers()
		{
			var types = ReflectionUtility.GetAllTypes<IAudioPlayer>(includeSelf: false);
			return types.Select(x => new ValueDropdownItem(x.Name, x.FullName));
		}
	}
}
