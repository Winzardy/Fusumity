using System;

namespace Fusumity.Editor.UserSettingsExtensions
{
	public readonly struct EditorUserSettingsEnumValue<TEnum> where TEnum : struct, Enum
	{
		private readonly EditorUserSettingsIntValue _intValue;

		public EditorUserSettingsEnumValue(string key, TEnum defaultValue)
		{
			_intValue = new EditorUserSettingsIntValue(key, Convert.ToInt32(defaultValue));
		}

		public TEnum Value
		{
			get
			{
				var intValue = _intValue.Value;
				return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
			}
			set => _intValue.Value = Convert.ToInt32(value);
		}
	}
}
