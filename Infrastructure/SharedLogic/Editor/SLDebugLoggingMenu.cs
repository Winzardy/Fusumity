using System;
using System.Collections.Generic;
using UnityEditor;

namespace SharedLogic.Editor
{
	//TODO: Объединить все Menu в один, используя либо EditorUserSettings или просто удобную обертку...
	[InitializeOnLoad]
	public static class SLDebugLoggingMenu
	{
		private const string PATH_LOGGING_COMMAND_EXECUTE = "Tools/Interop/Logging/Command/Execute";
		private const string PATH_LOGGING_SAVED = "Tools/Interop/Logging/Saved";
		private const string PATH_LOGGING_LOADED = "Tools/Interop/Logging/Loaded";

		private static readonly Dictionary<string, bool> _cache = new(2);
		private static readonly Dictionary<string, Action<bool>> _actions = new(2);

		static SLDebugLoggingMenu()
		{
			Register(PATH_LOGGING_COMMAND_EXECUTE, enable => SLDebug.Logging.Command.execute = enable, SLDebug.Logging.Command.execute);
			Register(PATH_LOGGING_SAVED, enable => SLDebug.Logging.saved = enable, SLDebug.Logging.saved);
			Register(PATH_LOGGING_LOADED, enable => SLDebug.Logging.loaded = enable, SLDebug.Logging.loaded);

			EditorApplication.delayCall += HandleDelayCall;

			void HandleDelayCall()
			{
				PerformAction(PATH_LOGGING_COMMAND_EXECUTE);
				PerformAction(PATH_LOGGING_SAVED);
				PerformAction(PATH_LOGGING_LOADED);
			}
		}

		private static void Register(string key, Action<bool> action, bool defaultValue = true)
		{
			_cache[key] = EditorPrefs.GetBool(key, defaultValue);
			_actions[key] = action;
		}

		private static void PerformAction(string path) => PerformAction(path, _cache[path]);
		private static void ToggleAction(string path) => PerformAction(path, !_cache[path]);

		private static void PerformAction(string path, bool enabled)
		{
			Menu.SetChecked(path, enabled);
			EditorPrefs.SetBool(path, enabled);
			_cache[path] = enabled;
			_actions[path]?.Invoke(enabled);
		}

		#region Menu

		[MenuItem(PATH_LOGGING_COMMAND_EXECUTE)]
		private static void ToggleActionCommandExecute() => ToggleAction(PATH_LOGGING_COMMAND_EXECUTE);

		[MenuItem(PATH_LOGGING_SAVED)]
		private static void ToggleActionSaved() => ToggleAction(PATH_LOGGING_SAVED);

		[MenuItem(PATH_LOGGING_LOADED)]
		private static void ToggleActionLoaded() => ToggleAction(PATH_LOGGING_LOADED);

		#endregion
	}
}
