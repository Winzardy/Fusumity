using Fusumity.Editor.UserSettingsExtensions;

namespace Fusumity.Editor.Assistance
{
	public static class InspectorDebug
	{
		private const string INSPECTOR_DEBUG_KEY = "InspectorDebug";
		private const string INSPECTOR_DEBUG_MENU_PATH = "Debug/Inspector Debug";

		private static readonly EditorUserSettingsBoolValue _inspectorDebug = new(INSPECTOR_DEBUG_KEY, false);

		[UnityEditor.MenuItem(INSPECTOR_DEBUG_MENU_PATH, validate = true)]
		private static bool ValidateInspectorDebug()
		{
			UnityEditor.Menu.SetChecked(INSPECTOR_DEBUG_MENU_PATH, IsEnabled());
			return true;
		}

		[UnityEditor.MenuItem(INSPECTOR_DEBUG_MENU_PATH)]
		private static void SwitchInspectorDebug()
		{
			_inspectorDebug.Value = !_inspectorDebug.Value;
		}

		public static bool IsEnabled()
		{
			return _inspectorDebug.Value;
		}
	}
}
