using System;
using System.Collections;
using System.Collections.Generic;
using Sapientia.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Utility
{
	public static class MonoScriptUtility
	{
		private static Dictionary<Type, MonoScript> _cache;
		private static Dictionary<string, MonoScript> _cacheByName;

		public static MonoScript FindMonoScriptByTypeName(this string typeName)
		{
			if (typeName.IsNullOrEmpty())
				return null;

			_cacheByName ??= new Dictionary<string, MonoScript>(16);
			if (_cacheByName.TryGetValue(typeName, out var cachedScript))
				return cachedScript;

			var scripts = AssetDatabaseUtility.GetAssets<MonoScript>();
			foreach (var script in scripts)
			{
				if (script.name == typeName)
				{
					_cacheByName[typeName] = script;
					return script;
				}
			}

			_cacheByName[typeName] = null;
			return null;
		}

		// TODO: MonoScript может содержать несколько типов, поэтому нужно уточнить поиск
		public static MonoScript FindMonoScript(this Type type)
		{
			if (type == null)
				return null;

			if (type.IsArray)
				type = type.GetElementType();

			if (type == null)
				return null;

			if (typeof(IList).IsAssignableFrom(type) && type.IsGenericType)
				type = type.GetGenericArguments()[0];

			var scripts = AssetDatabaseUtility.GetAssets<MonoScript>();

			_cache ??= new Dictionary<Type, MonoScript>(16);
			if (_cache.TryGetValue(type, out var cachedScript))
				return cachedScript;

			foreach (var script in scripts)
			{
				if (script.GetClass() == type)
				{
					_cache[type] = script;
					return script;
				}
			}

			foreach (var script in scripts)
			{
				var c = script.GetClass();

				if (c == null)
					continue;

				if (c.Assembly != type.Assembly)
					continue;

				if (c.Namespace != type.Namespace)
					continue;

				string str = null;
				if (type.IsInterface)
				{
					str = $"interface {type.Name}";
				}
				else
				{
					str = type.IsClass ? $"class {type.Name}" : $"struct {type.Name}";
				}

				if (script.text.Contains(str))
				{
					_cache[type] = script;
					return script;
				}
			}

			_cache = null;
			return null;
		}
	}
}
