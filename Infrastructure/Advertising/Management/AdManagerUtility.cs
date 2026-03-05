namespace Advertising
{
	public static class AdManagerUtility
	{
		public static bool CanShow<T>(this T entry, out AdShowError? error) where T : AdPlacementEntry
			=> AdManager.CanShow(entry, out error);

		public static bool Show<T>(this T entry, bool autoLoad = true) where T : AdPlacementEntry
			=> AdManager.Show(entry, autoLoad);

		public static bool Load<T>(this T entry) where T : AdPlacementEntry
			=> AdManager.Load(entry);
	}
}
