using System;
using System.Collections.Generic;
using UnityEditor;

namespace SharedLogic.Editor
{
	//TODO: Объединить все Menu в один, используя либо EditorUserSettings или просто удобную обертку...
	[InitializeOnLoad]
	public static class SLDebugLoggingMenu
	{
		private const string PATH_RECT_TRANSFORM_REBUILT = "Tools/Interop/Logging/Command/Execute";

		private static readonly Dictionary<string, bool> _cache = new(2);
		private static readonly Dictionary<string, Action<bool>> _actions = new(2);

		static SLDebugLoggingMenu()
		{
			Register(PATH_RECT_TRANSFORM_REBUILT, enable => SLDebug.Logging.Command.execute = enable, SLDebug.Logging.Command.execute);

			EditorApplication.delayCall += OnDelayCall;

			void OnDelayCall()
			{
				EditorApplication.delayCall -= OnDelayCall;
				PerformAction(PATH_RECT_TRANSFORM_REBUILT);
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

		[MenuItem(PATH_RECT_TRANSFORM_REBUILT)]
		private static void ToggleActionRectTransformRebuilt() => ToggleAction(PATH_RECT_TRANSFORM_REBUILT);

		#endregion
	}
}
