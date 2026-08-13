using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ContentConstantsSettingsAttributeProcessor : OdinAttributeProcessor<ContentConstantsSettings>
	{
		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(ContentConstantsSettings.useConstants):
					attributes.Add(new LabelTextAttribute("Generate Constants"));
					attributes.Add(new TooltipAttribute("Генерировать статический класс с константами по содержимому этого ассета"));
					break;

				case nameof(ContentConstantsSettings.className):
					AddOptionAttributes("Class Name", "Пусто — имя возьмётся из Id ассета (\"{Id}Keys\")");
					break;

				case nameof(ContentConstantsSettings.@namespace):
					AddOptionAttributes("Namespace", "Пусто — по умолчанию \"Content.Constants\"");
					break;

				case nameof(ContentConstantsSettings.outputPath):
					AddOptionAttributes("Output Path", "Пусто — по умолчанию Assets/Scripts/Content/Constants");
					attributes.Add(new FolderPathAttribute());
					break;

				case nameof(ContentConstantsSettings.generatedHash):
					attributes.Add(new HideInInspector());
					break;
			}

			void AddOptionAttributes(string label, string tooltip)
			{
				attributes.Add(new ShowIfAttribute(nameof(ContentConstantsSettings.useConstants)));
				attributes.Add(new LabelTextAttribute(label));
				attributes.Add(new TooltipAttribute(tooltip));
			}
		}
	}
}
