using Content;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using UnityEngine.Scripting;

namespace Advertising.Cheats
{
	[Preserve]
	internal static class AdvertisingCheatsUtility
	{
		public const string PATH = "App/Advertising/";
		public const string NONE = "None";

		public static string[] GetPlacements<T>()
			where T : AdPlacementEntry
		{
			using (ListPool<string>.Get(out var list))
			{
				foreach (var (id, _) in ContentManager.GetAllEntries<T>())
					list.Add(id);

				if (list.IsEmpty())
					list.Add(NONE);

				return list.ToArray();
			}
		}

		public static void RequestShow(AdPlacementType type, string placement)
		{
			if (placement == NONE)
				return;

			if (placement.IsNullOrEmpty())
				return;

			if (!AdManager.CanShow(type, placement, out var error) && error != AdShowErrorCode.NotLoaded)
			{
				AdsDebug.LogError($"[{type}] Failed to show [ {placement} ]: {error} ");
				return;
			}

			AdManager.Show(type, placement);
		}

		public static void LogCanShow(AdPlacementType type, string placement)
		{
			if (placement == NONE)
				return;

			if (placement.IsNullOrEmpty())
				return;

			var result = AdManager.CanShow(type, placement, out var error);
			var errorText = error.HasValue ? $", error: {error.ToString()}" : string.Empty;
			AdsDebug.Log($"[{type}] Result check show: {result}{errorText} ");
		}
	}
}
