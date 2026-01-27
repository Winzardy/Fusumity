namespace Localization
{
	public partial class LocUtility
	{
		public const string SHORT_DESCRIPTION_LOC_KEY_POSTFIX = "_shortdesc";
		public const string DESCRIPTION_LOC_KEY_POSTFIX = "_fulldesc";

		public static string ToDescriptionKey(this string key) => $"{key}{DESCRIPTION_LOC_KEY_POSTFIX}";
		public static string ToShortDescriptionKey(this string key) => $"{key}{SHORT_DESCRIPTION_LOC_KEY_POSTFIX}";

		public static LocKey ToDescriptionKey(this in LocKey key) => key.value.ToDescriptionKey();
		public static LocKey ToShortDescriptionKey(this in LocKey key) => key.value.ToShortDescriptionKey();

		public static string ToDescriptionLocalize(this in LocKey key) => key.ToDescriptionKey().ToLocalize();
		public static string ToShortDescriptionLocalize(this in LocKey key) => key.ToShortDescriptionKey().ToLocalize();
	}
}
