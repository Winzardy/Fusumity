using System;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor
{
	public static partial class FusumityEditorGUILayout
	{
		public class SettingsProviderScope : IDisposable
		{
			private static readonly GUIStyle _style = CreateStyle();

			public SettingsProviderScope()
			{
				SirenixEditorGUI.BeginIndentedVertical(_style);
			}

			public void Dispose()
			{
				SirenixEditorGUI.EndIndentedVertical();
			}

			private static GUIStyle CreateStyle()
			{
				var style = new GUIStyle();
				var offset = style.margin;
				offset.top += 3;
				offset.left += 10;
				offset.right += 3;
				style.margin = offset;
				return style;
			}
		}
	}
}
