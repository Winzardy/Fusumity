namespace Localizations
{
	public static class LocKeyExt
	{
		public static bool HasLocalize(this in LocKey key)
			=> key.value.HasLocalize();

		public static string ToLocalize(this in LocKey key)
			=> key.value.ToLocalize();

		public static string ToLocalize(this in LocKey key, params object[] args)
			=> key.value.ToLocalize(args);

		public static string ToLocalize(this in LocKey key, string tag, string value)
			=> key.value.ToLocalize(tag, value);

		public static string ToLocalize(this in LocKey key, params (string tag, string value)[] tags)
			=> key.value.ToLocalize(tags);

		public static bool IsEmpty(this in LocKey key)
			=> LocalizationExt.IsEmpty(key.value);
	}
}
