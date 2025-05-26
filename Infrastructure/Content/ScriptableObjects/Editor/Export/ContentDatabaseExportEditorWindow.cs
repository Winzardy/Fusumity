using Fusumity.Utility;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ContentDatabaseExportEditorWindow : OdinEditorWindow
	{
		public static void Open()
		{
			var window = GetWindow<ContentDatabaseExportEditorWindow>();
			var title = nameof(ContentDatabaseExport).NicifyText();
			window.titleContent = new GUIContent(title);
			window.Show();
		}

		protected override object GetTarget() => ContentDatabaseExport.Asset;
	}
}
