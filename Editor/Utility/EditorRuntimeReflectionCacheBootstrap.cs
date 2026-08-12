#if RUNTIME_REFLECTION_CACHE
using Sapientia.Collections;
using Sapientia.Utility;
using System.Linq;
using UnityEditor;

namespace Fusumity.Utilities.Editor
{
	public static class EditorRuntimeReflectionCacheBootstrap
	{
		[InitializeOnLoadMethod]
		public static void Initialize()
		{
			if (ReflectionUtility.HasCache)
				return;

			var allTags = ReflectionUtility
				.AllowedAssemblyTags.ToArray()
				.Add(ReflectionUtility.EditorAssemblyTag);

			var cache = new RuntimeReflectionCache(allTags);
			ReflectionUtility.SetCache(cache);
		}
	}
}
#endif
