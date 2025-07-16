namespace Localization
{
	public static class TextLocalizationArgsUtility
	{
		public static bool IsNullOrEmpty(this TextLocalizationArgs args)
			=> args == null || LocUtility.IsEmpty(args.key);
	}
}
