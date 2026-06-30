namespace Localization
{
	public partial class LocUtility
	{
		public const string SHORT_DESCRIPTION_LOC_KEY_POSTFIX = "_shortdesc";
		public const string DESCRIPTION_LOC_KEY_POSTFIX = "_fulldesc";
		public const string TOOLTIP_LOC_KEY_POSTFIX = "_tooltip";

		public static string ToDescriptionKey(this string key) => $"{key}{DESCRIPTION_LOC_KEY_POSTFIX}";
		public static string ToShortDescriptionKey(this string key) => $"{key}{SHORT_DESCRIPTION_LOC_KEY_POSTFIX}";
		public static string ToTooltipKey(this string key) => $"{key}{TOOLTIP_LOC_KEY_POSTFIX}";

		public static LocKey ToDescriptionKey(this in LocKey key) => key.value.ToDescriptionKey();
		public static LocKey ToShortDescriptionKey(this in LocKey key) => key.value.ToShortDescriptionKey();
		public static LocKey ToTooltipKey(this in LocKey key) => key.value.ToTooltipKey();
		public static LocKey ToTooltipKeySafe(this in LocKey key) => key.IsEmpty() ? null : key.value.ToTooltipKey();

		public static string ToDescriptionLocalize(this in LocKey key) => key.ToDescriptionKey().ToLocalize();
		public static string ToShortDescriptionLocalize(this in LocKey key) => key.ToShortDescriptionKey().ToLocalize();
		public static string ToTooltipLocalize(this in LocKey key) => key.ToTooltipKey().ToLocalize();
	}
}
