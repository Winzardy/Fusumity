using UnityEditor;

namespace Fusumity.Utility.Editor
{
	public static class LocaSaveEditorMenu
	{
		[MenuItem("Tools/Other/Local Save/Clear All")]
		public static void ClearAll() => LocalSave.ClearAll();
	}
}
