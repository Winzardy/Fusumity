using UnityEditor;

namespace Fusumity.Editor.UserSettingsExtensions
{
	public readonly struct EditorUserSettingsUshortValue
	{
		private readonly ushort _defaultValue;
		private readonly string _key;

		public EditorUserSettingsUshortValue(string key, ushort defaultValue)
		{
			_key = key;
			_defaultValue = defaultValue;
		}

		public ushort Value
		{
			get
			{
				var configValue = EditorUserSettings.GetConfigValue(_key);
				if (ushort.TryParse(configValue, out var value))
					return value;
				return _defaultValue;
			}
			set => EditorUserSettings.SetConfigValue(_key, value.ToString());
		}
	}
}
