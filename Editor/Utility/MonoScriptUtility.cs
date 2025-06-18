using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusumity.Editor.Extensions;
using Sapientia.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Utility
{
	public static class MonoScriptUtility
	{
		private static Dictionary<string, MonoScript> _pathToScript;
		private static Dictionary<Type, MonoScript> _typeToScript;
		private static Dictionary<string, HashSet<Type>> _scriptToTypes;
		private static Dictionary<string, MonoScript> _typeNameToScript;

		public static MonoScript FindMonoScriptByTypeName(this string typeName)
		{
			if (typeName.IsNullOrEmpty())
				return null;

			_typeNameToScript ??= new Dictionary<string, MonoScript>(16);
			if (_typeNameToScript.TryGetValue(typeName, out var cachedScript))
				return cachedScript;

			var scripts = AssetDatabaseUtility.GetAssets<MonoScript>();
			foreach (var script in scripts)
			{
				if (script.name == typeName)
				{
					_typeNameToScript[typeName] = script;
					return script;
				}
			}

			_typeNameToScript[typeName] = null;
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

			_pathToScript ??= AssetDatabaseUtility.GetAssets<MonoScript>()
			   .ToDictionary(AssetDatabase.GetAssetPath, x => x);

			_typeToScript ??= new Dictionary<Type, MonoScript>(16);
			if (_typeToScript.TryGetValue(type, out var cachedScript))
				return cachedScript;

			foreach (var script in _pathToScript.Values)
			{
				if (script.GetClass() == type)
				{
					_typeToScript[type] = script;

					var scriptPath = script.GetAssetPath();

					_scriptToTypes ??= new Dictionary<string, HashSet<Type>>(16);

					_scriptToTypes.TryAdd(scriptPath, new HashSet<Type>(16));
					_scriptToTypes[scriptPath].Add(type);
					return script;
				}
			}

			foreach (var script in _pathToScript.Values)
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
					_typeToScript[type] = script;
					var scriptPath = script.GetAssetPath();
					_scriptToTypes ??= new Dictionary<string, HashSet<Type>>(16);
					_scriptToTypes.TryAdd(scriptPath, new HashSet<Type>(16));
					_scriptToTypes[scriptPath].Add(type);

					return script;
				}
			}

			_typeToScript = null;
			return null;
		}

		public static MonoScript GetMonoScript(string path)
		{
			return _pathToScript[path];
		}

		public static HashSet<Type> GetTypes(string path)
		{
			return _scriptToTypes[path];
		}

		public static HashSet<Type> GetTypes(MonoScript script)
		{
			return _scriptToTypes[script.GetAssetPath()];
		}

		public static void ClearCache()
		{
			_pathToScript.Clear();

			foreach (var hashSet in _scriptToTypes.Values)
				hashSet.Clear();

			_scriptToTypes.Clear();
		}
	}
}
