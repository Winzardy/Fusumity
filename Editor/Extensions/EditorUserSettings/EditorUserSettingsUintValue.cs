using UnityEditor;

namespace Fusumity.Editor.UserSettingsExtensions
{
	public readonly struct EditorUserSettingsUintValue
	{
		private readonly uint _defaultValue;
		private readonly string _key;

		public EditorUserSettingsUintValue(string key, uint defaultValue)
		{
			_key = key;
			_defaultValue = defaultValue;
		}

		public uint Value
		{
			get
			{
				var configValue = EditorUserSettings.GetConfigValue(_key);
				if (uint.TryParse(configValue, out var value))
					return value;
				return _defaultValue;
			}
			set => EditorUserSettings.SetConfigValue(_key, value.ToString());
		}
	}
}
