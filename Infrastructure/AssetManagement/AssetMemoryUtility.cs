using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace AssetManagement
{
	using UnityObject = UnityEngine.Object;

	public static class AssetMemoryUtility
	{
		/// <summary>
		/// Типы, у которых нативный размер отражает реальное потребление памяти
		/// </summary>
		private static readonly Type[] NATIVE_SIZED_TYPES =
		{
			typeof(Texture),
			typeof(Sprite),
			typeof(AudioClip),
			typeof(Mesh),
			typeof(Font),
			typeof(AnimationClip),
			typeof(TextAsset)
		};

		public static bool HasMeaningfulSize(UnityObject asset)
		{
			if (asset == null)
				return false;

			var assetType = asset.GetType();
			foreach (var nativeSizedType in NATIVE_SIZED_TYPES)
			{
				if (nativeSizedType.IsAssignableFrom(assetType))
					return true;
			}

			return false;
		}

		/// <returns>false, если для типа ассета нативный размер не показателен</returns>
		public static bool TryGetSize(UnityObject asset, out long size)
		{
			if (!HasMeaningfulSize(asset))
			{
				size = 0L;
				return false;
			}

			size = Profiler.GetRuntimeMemorySizeLong(asset);
			return true;
		}

		/// <summary>
		/// Суммарный размер ассета или коллекции ассетов, без учёта непоказательных типов
		/// </summary>
		public static long GetSize(object asset)
		{
			switch (asset)
			{
				case UnityObject unityAsset:
					return TryGetSize(unityAsset, out var size) ? size : 0L;

				case string:
					return 0L;

				case IEnumerable assets:
					var total = 0L;
					foreach (var item in assets)
					{
						if (item is UnityObject collectionAsset)
							total += TryGetSize(collectionAsset, out var itemSize) ? itemSize : 0L;
					}

					return total;

				default:
					return 0L;
			}
		}

		public static string FormatBytes(long bytes)
		{
			if (bytes >= 1024L * 1024L)
				return (bytes / (1024f * 1024f)).ToString("0.0") + " MB";

			if (bytes >= 1024L)
				return (bytes / 1024f).ToString("0.0") + " KB";

			return bytes + " B";
		}
	}
}
