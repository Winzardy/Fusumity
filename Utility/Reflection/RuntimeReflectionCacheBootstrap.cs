#if RUNTIME_REFLECTION_CACHE
using Sapientia.Collections;
using Sapientia.Utility;
using System.Linq;
using UnityEngine;

namespace Fusumity.Utilities.Client
{
    public static class RuntimeReflectionCacheBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Initialize()
        {
            if (ReflectionUtility.HasCache) return;

			var allTags = ReflectionUtility
				.AllowedAssemblyTags.ToArray()
				.Add(ReflectionUtility.EditorAssemblyTag);

			var cache = new RuntimeReflectionCache(allTags);
            ReflectionUtility.SetCache(cache);

#if UNITY_EDITOR
            cache.Warmup();
#else
            System.Threading.Tasks.Task.Run(() => cache.Warmup());
#endif
        }
    }
}
#endif
