using System;
using System.Collections.Generic;
using UnityEngine;

namespace WLog
{
    public static class WLogContextHolder
    {
        private static readonly List<WLogContext> _allContexts = new(32);
        private static readonly Dictionary<Type, WLogContext> _typedContexts = new(32);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void CleanUp()
        {
            _allContexts.Clear();
            _typedContexts.Clear();
        }

        internal static WLogContext GetOrCreate(Type type)
        {
            if (_typedContexts.TryGetValue(type, out var typedContext))
            {
                return typedContext;
            }

            var (context, created) = GetOrCreateInternal(type.Name);
            if (created)
            {
                _typedContexts.Add(type, context);
            }

            return context;
        }

        internal static WLogContext GetOrCreate(string name)
        {
            return GetOrCreateInternal(name).context;
        }

        public static WLogContext GetLogContext<T>(T context)
        {
            return context as WLogContext ?? GetOrCreate(typeof(T));
        }

        public static List<WLogContext> GetAllContexts()
            => _allContexts;

        private static (WLogContext context, bool created) GetOrCreateInternal(string name)
        {
            var context = _allContexts.Find(x => x.Name == name);
            if (context != null)
            {
                return (context, false);
            }

            context = new WLogContext(name);

            _allContexts.Add(context);

            return (context, true);
        }
    }
}
