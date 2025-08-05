using Content;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using UnityEngine.Scripting;

namespace InAppPurchasing.Cheats
{
	[Preserve]
	internal static class InAppPurchasingCheatsUtility
	{
		public const string NONE = "None";

		public const string COMMAND_PATH = "App/IAP/";
		public const string SETTINGS_PATH = "System/IAP";

		public static string[] GetProducts<T>()
			where T : IAPProductEntry
		{
			using (ListPool<string>.Get(out var list))
			{
				foreach (var (id, _) in ContentManager.GetAll<T>())
					list.Add(id);

				if (list.IsEmpty())
					list.Add(NONE);

				return list.ToArray();
			}
		}

		public static IAPProductEntry[] GetProducts()
		{
			using (ListPool<IAPProductEntry>.Get(out var list))
			{
				foreach (var contentEntry in ContentManager.GetAll<IAPConsumableProductEntry>())
					list.Add(contentEntry.Value);
				foreach (var contentEntry in ContentManager.GetAll<IAPNonConsumableProductEntry>())
					list.Add(contentEntry.Value);
				foreach (var contentEntry in ContentManager.GetAll<IAPSubscriptionProductEntry>())
					list.Add(contentEntry.Value);

				return list.ToArray();
			}
		}

		public static void RequestPurchase(IAPProductType type, string product)
		{
			if (product == NONE)
				return;

			if (product.IsNullOrEmpty())
				return;

			if (!IAPManager.CanPurchase(type, product, out var error))
			{
				IAPDebug.LogError($"[{type}] Failed to purchase [ {product} ]: {error}");
				return;
			}

			IAPManager.RequestPurchase(type, product);
		}

		public static void LogCanPurchase(IAPProductType type, string product)
		{
			if (product == NONE)
				return;

			if (product.IsNullOrEmpty())
				return;

			var result = IAPManager.CanPurchase(type, product, out var error);
			var errorText = error.HasValue ? $", error: {error.ToString()}" : string.Empty;
			IAPDebug.Log($"[{type}] Result check purchase: {result}{errorText}");
		}
	}
}
