namespace Localizations
{
	public static class TextLocalizationArgsExt
	{
		public static bool IsNullOrEmpty(this TextLocalizationArgs args)
			=> args == null || LocalizationExt.IsEmpty(args.key);
	}
}
