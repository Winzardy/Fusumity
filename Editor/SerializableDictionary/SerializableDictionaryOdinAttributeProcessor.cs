using System;
using System.Collections.Generic;
using Fusumity.Collections;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor
{
	public class SerializableDictionaryOdinAttributeProcessor : OdinAttributeProcessor<ISerializableDictionary>
	{
		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new CustomContextMenuAttribute(
				"Sync",
				$"@{nameof(SerializableDictionaryOdinAttributeProcessor)}.{nameof(Sync)}($property)"));
		}

		public static void Sync(InspectorProperty property)
		{
			if (property.ValueEntry.WeakSmartValue is not ISerializableDictionary dictionary)
				return;

			dictionary.Sync();
		}
	}
}
