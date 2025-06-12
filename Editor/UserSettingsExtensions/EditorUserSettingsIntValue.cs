using UnityEditor;

namespace Fusumity.Editor.UserSettingsExtensions
{
	public readonly struct EditorUserSettingsIntValue
	{
		private readonly int _defaultValue;
		private readonly string _key;

		public EditorUserSettingsIntValue(string key, int defaultValue)
		{
			_key = key;
			_defaultValue = defaultValue;
		}

		public int Value
		{
			get
			{
				var configValue = EditorUserSettings.GetConfigValue(_key);
				if (int.TryParse(configValue, out var value))
					return value;
				return _defaultValue;
			}
			set => EditorUserSettings.SetConfigValue(_key, value.ToString());
		}
	}
}
