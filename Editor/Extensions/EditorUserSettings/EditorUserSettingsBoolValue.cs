using UnityEditor;

namespace Fusumity.Editor.Utility
{
	public readonly struct EditorUserSettingsBoolValue
	{
		private readonly bool _defaultValue;
		private readonly string _key;

		public EditorUserSettingsBoolValue(string key, bool defaultValue)
		{
			_key = key;
			_defaultValue = defaultValue;
		}

		public bool Value
		{
			get
			{
				string configValue = EditorUserSettings.GetConfigValue(_key);
				if (string.IsNullOrEmpty(configValue))
					return _defaultValue;
				return configValue == "true";
			}
			set => EditorUserSettings.SetConfigValue(_key, value ? "true" : "false");
		}
	}
}
