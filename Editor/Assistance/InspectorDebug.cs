using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	public static class InspectorDebug
	{
		public const string INSPECTOR_DEBUG_KEY = "InspectorDebug";
		private const string INSPECTOR_DEBUG_MENU_PATH = "Debug/Inspector Debug";

		[UnityEditor.MenuItem(INSPECTOR_DEBUG_MENU_PATH, validate = true)]
		private static bool ValidateInspectorDebug()
		{
			UnityEditor.Menu.SetChecked(INSPECTOR_DEBUG_MENU_PATH, IsEnabled());
			return true;
		}

		[UnityEditor.MenuItem(INSPECTOR_DEBUG_MENU_PATH)]
		private static void SwitchInspectorDebug()
		{
			PlayerPrefs.SetInt(INSPECTOR_DEBUG_KEY, IsEnabled() ? 0 : 1);
			PlayerPrefs.Save();
		}

		public static bool IsEnabled()
		{
			return PlayerPrefs.GetInt(INSPECTOR_DEBUG_KEY, 0) == 1;
		}
	}
}
