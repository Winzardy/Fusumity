using System;
using System.Collections.Generic;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	//TODO: Объединить все Menu в один, используя либо EditorUserSettings или просто удобную обертку...
	[InitializeOnLoad]
	public static class ContentDebugLoggingMenu
	{
		private const string PATH_DATABASE = ContentMenuConstants.LOG_MENU + "Database";

		private const string PATH_NESTED_RESTORE = ContentMenuConstants.LOG_NESTED_ENTRY_MENU + "Restore";
		private const string PATH_NESTED_REGENERATE = ContentMenuConstants.LOG_NESTED_ENTRY_MENU + "Regenerate";
		private const string PATH_NESTED_REFRESH = ContentMenuConstants.LOG_NESTED_ENTRY_MENU + "Refresh";

		private static readonly Dictionary<string, bool> _cache = new(2);
		private static readonly Dictionary<string, Action<bool>> _actions = new(2);

		#region Menu

		[MenuItem(PATH_DATABASE)]
		private static void ToggleLogDatabase() => ToggleLog(PATH_DATABASE);

		[MenuItem(PATH_NESTED_RESTORE)]
		private static void ToggleLogNestedRestore() => ToggleLog(PATH_NESTED_RESTORE);

		[MenuItem(PATH_NESTED_REGENERATE)]
		private static void ToggleLogNestedRegenerate() => ToggleLog(PATH_NESTED_REGENERATE);

		[MenuItem(PATH_NESTED_REFRESH)]
		private static void ToggleLogNestedRefresh() => ToggleLog(PATH_NESTED_REFRESH);

		#endregion

		static ContentDebugLoggingMenu()
		{
			Register(PATH_DATABASE, enable => ContentDebug.Logging.database = enable,
				ContentDebug.Logging.database);

			Register(PATH_NESTED_RESTORE, enable => ContentDebug.Logging.Nested.restore = enable,
				ContentDebug.Logging.Nested.restore);
			Register(PATH_NESTED_REGENERATE, enable => ContentDebug.Logging.Nested.regenerate = enable,
				ContentDebug.Logging.Nested.regenerate);
			Register(PATH_NESTED_REFRESH, enable => ContentDebug.Logging.Nested.refresh = enable,
				ContentDebug.Logging.Nested.refresh);

			EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				PerformAction(PATH_DATABASE);

				PerformAction(PATH_NESTED_RESTORE);
				PerformAction(PATH_NESTED_REGENERATE);
				PerformAction(PATH_NESTED_REFRESH);
			}
		}

		private static void Register(string key, Action<bool> action, bool defaultValue = true)
		{
			_cache[key] = EditorPrefs.GetBool(key, defaultValue);
			_actions[key] = action;
		}

		private static void PerformAction(string path) => PerformAction(path, _cache[path]);
		private static void ToggleLog(string path) => PerformAction(path, !_cache[path]);

		private static void PerformAction(string path, bool enabled)
		{
			Menu.SetChecked(path, enabled);
			EditorPrefs.SetBool(path, enabled);
			_cache[path] = enabled;
			_actions[path]?.Invoke(enabled);
		}
	}
}
