using System;
using Sapientia.Extensions;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public static class ContentConstantsMenu
	{
		[MenuItem(ContentMenuConstants.CONSTANTS_MENU + "Settings", priority = 90)]
		private static void OpenGenerateSettings()
		{
			SettingsService.OpenProjectSettings(ContentConstantGeneratorSettings.SETTINGS_PROVIDER_PATH);
		}
	}

	[InitializeOnLoad]
	public static class ContentAutoConstantsGeneratorMenu
	{
		private const string AUTO_MENU_PATH = ContentMenuConstants.CONSTANTS_MENU + "Auto";

		private const string SAVE_LAST_PATH = "AutoGenerateContentConstants";

		private const int DELAY_MS = 120000;
		private static readonly long DELAY = DELAY_MS.ToTicks();
		private static bool _enable;
		public static bool IsEnable => _enable && !CheckBlockGenerateByDelay();

		[MenuItem(AUTO_MENU_PATH, priority = 100)]
		private static void Toggle() => Toggle(!_enable);

		static ContentAutoConstantsGeneratorMenu()
		{
			_enable = EditorPrefs.GetBool(AUTO_MENU_PATH, false);
			EditorApplication.delayCall += OnDelayCall;

			void OnDelayCall()
			{
				EditorApplication.delayCall -= OnDelayCall;
				Toggle(_enable);
			}
		}

		public static void Reset() => EditorPrefs.SetString(SAVE_LAST_PATH, 0.ToString());

		public static void UpdateBlockGenerate()
		{
			var utcNowTicks = DateTime.UtcNow.Ticks;
			EditorPrefs.SetString(SAVE_LAST_PATH, utcNowTicks.ToString());
		}

		private static void Toggle(bool enabled)
		{
			Menu.SetChecked(AUTO_MENU_PATH, enabled);
			EditorPrefs.SetBool(AUTO_MENU_PATH, enabled);
			_enable = enabled;
		}

		private static bool CheckBlockGenerateByDelay()
		{
			var last = EditorPrefs.GetString(SAVE_LAST_PATH, 0.ToString());

			var utcNowTicks = DateTime.UtcNow.Ticks;
			if (long.TryParse(last, out var ticks))
			{
				var diff = utcNowTicks - ticks;
				var diffSec = TimeSpan.FromTicks(diff).TotalSeconds;
				var delaySec = TimeSpan.FromTicks(DELAY).TotalSeconds;

				if (diff > DELAY)
				{
					//TODO: LocalEditorSave!
					UpdateBlockGenerate();
					return true;
				}

				ContentDebug.Log($"Block auto-generate constants: diff [ {diffSec} sec ], delay [ {delaySec} sec ]");
			}

			return false;
		}
	}
}
