using System;
using System.Globalization;
using Fusumity.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Survivor.Interop;
using UnityEngine;

namespace Sapientia.Data.Time.Editor
{
	public class SerializableDateTimeDrawer : OdinValueDrawer<SerializableDateTime>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var value = ValueEntry.SmartValue;

			var dateTimeStr = SirenixEditorFields.TextField(label, value.ToString(CultureInfo.InvariantCulture));
			if (DateTime.TryParse(dateTimeStr, out var parsed))
				value = parsed;

			FusumityEditorGUILayout.SuffixLabel("utc", true);
			ValueEntry.SmartValue = value;
		}
	}
}
