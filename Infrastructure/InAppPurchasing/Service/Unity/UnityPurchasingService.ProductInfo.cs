using System;
using System.Collections.Generic;
using Sapientia.Extensions;

namespace InAppPurchasing.Unity
{
	using UnityProduct = UnityEngine.Purchasing.Product;

	public partial class UnityPurchasingService
	{
		private const int DELAY_UPDATING_PRODUCT_CACHE_MS = 10000; // 10 secs
		private static readonly long DELAY_UPDATING_PRODUCT_CACHE_TICKS = DELAY_UPDATING_PRODUCT_CACHE_MS.ToTicks();

		private Dictionary<IAPProductEntry, UnityProductCache> _productToCache;

		/// <summary>
		/// Получить актуальную информацию о продукте с платформы
		/// </summary>
		/// <param name="forceUpdateCache">при <c>true</c> форсит обновление кеша</param>
		/// <remarks>
		/// От частых запросов к платформе, возвращает кэшированную информацию,
		/// можно зафорсировать обновление кеша.
		/// Кеш хранится <c>10 секунд</c> (<see cref="DELAY_MS"/>)
		/// </remarks>
		public ref readonly ProductInfo GetProductInfo(IAPProductEntry entry, bool forceUpdateCache = false)
			=> ref GetProductCache(entry, out _, forceUpdateCache).info;

		private UnityProductCache GetProductCache(IAPProductEntry product, out ProductUpdateFailureReason failureReason,
			bool force)
		{
			failureReason = ProductUpdateFailureReason.None;

			_productToCache ??= new Dictionary<IAPProductEntry, UnityProductCache>(2);

			if (_productToCache.TryGetValue(product, out var cache))
			{
				if (!force && DateTime.Now.Ticks - cache.timeTicks < DELAY_UPDATING_PRODUCT_CACHE_TICKS)
					return cache;
			}
			else
			{
				_productToCache[product] = new();
			}

			if (!UpdateInfo(product, out failureReason))
				IAPDebug.LogWarning($"Failed to collect product by id [ {product.Id} ] with reason [ {failureReason} ]");

			return _productToCache[product];
		}

		private bool UpdateInfo(IAPProductEntry product, out ProductUpdateFailureReason reason)
		{
			reason = ProductUpdateFailureReason.None;

			if (!TryGetUnityProduct(product, out var unityProduct))
			{
				reason = ProductUpdateFailureReason.UnityProductNotFound;
				return false;
			}

			var cache = _productToCache[product];
			cache.timeTicks = DateTime.Now.Ticks;
			cache.info = unityProduct.Convert(product.price);
			cache.rawInfo = unityProduct;

			return true;
		}
	}

	internal enum ProductUpdateFailureReason
	{
		None,

		UnityProductNotFound
	}

	internal class UnityProductCache
	{
		public long timeTicks;
		public ProductInfo info;

		public UnityProduct rawInfo;
	}
}
