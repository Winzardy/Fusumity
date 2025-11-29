using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace Fusumity.Editor.Utility
{
	public static class EditorAssetsCache
	{
		private static Dictionary<Type, Object[]> _cachedAssets;
		private static Dictionary<Type, Dictionary<string, IIdentifiable>> _cachedIdentifiables;

		public static T GetCachedIdentifiable<T>(string id) where T : class, IIdentifiable
		{
			TryGetCachedIdentifiable(id, out T identifiable);
			return identifiable;
		}

		public static bool TryGetCachedIdentifiable<T>(string id, out T identifiable) where T : class, IIdentifiable
		{
			if (id.IsNullOrEmpty())
			{
				identifiable = null;
				return false;
			}

			if (TryGetCachedIdentifiable(typeof(T), id, out var found))
			{
				identifiable = found as T;
				return identifiable != null;
			}

			identifiable = null;
			return false;
		}

		public static bool TryGetCachedIdentifiable(Type type, string id, out IIdentifiable identifiable)
		{
			if (!typeof(IIdentifiable).IsAssignableFrom(type))
			{
				identifiable = null;
				return false;
			}

			if (_cachedIdentifiables == null)
			{
				_cachedIdentifiables = new Dictionary<Type, Dictionary<string, IIdentifiable>>();
			}

			if (!_cachedIdentifiables.TryGetValue(type, out var identifiables))
			{
				var assets = GetCachedAssets(type)?.Cast<IIdentifiable>();

				if (assets != null)
				{
					try
					{
						identifiables = assets.ToDictionary(x => x.Id, x => x);
						_cachedIdentifiables[type] = identifiables;
					}
					catch (ArgumentException)
					{
						UnityEngine.Debug.LogError(
							$"Found duplicate id entries for [ {type.Name} ] - " +
							$"{assets.GetCompositeString(x => x.Id)}");
					}
					catch (Exception e)
					{
						UnityEngine.Debug.LogError(
							$"Could not cache identifiable of type [{type.Name}] - " +
							$"{e.Message}");
					}
				}
			}

			if (identifiables != null)
			{
				return identifiables.TryGetValue(id, out identifiable);
			}

			identifiable = null;
			return false;
		}

		public static T GetCachedAsset<T>(Func<T, bool> predicate) where T : Object
		{
			var objs = GetCachedAssets(typeof(T));

			if (objs.IsNullOrEmpty())
			{
				UnityEngine.Debug.LogError($"Could not find asset of type: [ {typeof(T).Name} ]");
				return null;
			}

			foreach (var obj in objs)
			{
				var cast = obj as T;
				if (predicate.Invoke(cast))
				{
					return cast;
				}
			}

			return null;
		}

		public static T GetCachedAsset<T>() where T : Object
		{
			var objs = GetCachedAssets(typeof(T));

			if (objs.IsNullOrEmpty())
			{
				UnityEngine.Debug.LogError($"Could not find asset of type: [ {typeof(T).Name} ]");
				return null;
			}

			var instance = objs.First();

			return instance as T;
		}

		public static bool TryGetCachedAsset<T>(out T asset) where T : Object
		{
			var objs = GetCachedAssets(typeof(T));

			if (objs.IsNullOrEmpty())
			{
				asset = null;
				return false;
			}

			asset = objs.First() as T;
			return asset != null;
		}

		public static Object[] GetCachedAssets(Type type)
		{
			if (_cachedAssets == null)
			{
				_cachedAssets = new Dictionary<Type, Object[]>();
			}

			if (!_cachedAssets.TryGetValue(type, out var assets))
			{
				assets = AssetDatabaseUtility.GetAssets(type);
				_cachedAssets[type] = assets;
			}

			return assets;
		}

		public static T[] GetCachedAssets<T>() where T : Object
		{
			var assets = GetCachedAssets(typeof(T));
			var typedAssets = new T[assets.Length];
			Array.Copy(assets, typedAssets, assets.Length);
			return typedAssets;
		}

		public static void RefreshCache()
		{
			if (_cachedAssets.IsNullOrEmpty())
				return;

			foreach (var type in new List<Type>(_cachedAssets.Keys))
			{
				RefreshAssetsTypeInternal(type);
			}
		}

		public static void RefreshAsset<T>() where T : Object
		{
			RefreshAsset(typeof(T));
		}

		public static void RefreshAsset(Type type)
		{
			if (_cachedAssets.IsNullOrEmpty())
				return;

			if (_cachedAssets.ContainsKey(type))
			{
				RefreshAssetsTypeInternal(type);
			}
		}

		private static void RefreshAssetsTypeInternal(Type type)
		{
			var assets = AssetDatabaseUtility.GetAssets(type);
			_cachedAssets[type] = assets;

			if (_cachedIdentifiables != null &&
				_cachedIdentifiables.ContainsKey(type))
			{
				var identifiables = assets.Cast<IIdentifiable>();
				if (identifiables != null)
				{
					_cachedIdentifiables[type] = identifiables.ToDictionary(x => x.Id, x => x);
				}
			}
		}

		public static void ResetCache()
		{
			_cachedAssets?.Clear();
			_cachedIdentifiables?.Clear();
		}
	}
}
