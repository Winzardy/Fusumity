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

			var scriptType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
			var scriptTypeName = scriptType.GetScriptTypeName();

			_pathToScript ??= AssetDatabaseUtility.GetAssets<MonoScript>()
			   .ToDictionary(AssetDatabase.GetAssetPath, x => x);

			_typeToScript ??= new Dictionary<Type, MonoScript>(16);
			if (_typeToScript.TryGetValue(type, out var cachedScript))
				return cachedScript;

			if (scriptType != type && _typeToScript.TryGetValue(scriptType, out cachedScript))
			{
				CacheScript(type, scriptType, cachedScript);
				return cachedScript;
			}

			foreach (var script in _pathToScript.Values)
			{
				var scriptClass = script.GetClass();
				if (scriptClass == type || scriptClass == scriptType)
				{
					CacheScript(type, scriptType, script);
					return script;
				}
			}

			foreach (var script in _pathToScript.Values)
			{
				var c = script.GetClass();

				if (c != null && c.Assembly != scriptType.Assembly)
					continue;

				if (c != null && c.Namespace != scriptType.Namespace)
					continue;

				if (c == null && script.name != scriptTypeName)
					continue;

				if (script.text.ContainsTypeDeclaration(scriptType, scriptTypeName))
				{
					CacheScript(type, scriptType, script);
					return script;
				}
			}

			CacheScript(type, scriptType, null);
			return null;
		}

		private static void CacheScript(Type type, Type scriptType, MonoScript script)
		{
			_typeToScript[type] = script;

			if (scriptType != type)
				_typeToScript[scriptType] = script;

			if (!script)
				return;

			var scriptPath = script.GetAssetPath();
			_scriptToTypes ??= new Dictionary<string, HashSet<Type>>(16);
			_scriptToTypes.TryAdd(scriptPath, new HashSet<Type>(16));
			_scriptToTypes[scriptPath].Add(type);

			if (scriptType != type)
				_scriptToTypes[scriptPath].Add(scriptType);
		}

		private static string GetScriptTypeName(this Type type)
		{
			var name = type.Name;
			var genericMarkerIndex = name.IndexOf('`');
			return genericMarkerIndex >= 0 ? name[..genericMarkerIndex] : name;
		}

		private static bool ContainsTypeDeclaration(this string text, Type type, string typeName)
		{
			var declaration = $"{type.GetTypeDeclarationKeyword()} {typeName}";
			var index = -1;

			while ((index = text.IndexOf(declaration, index + 1, StringComparison.Ordinal)) >= 0)
			{
				var nextCharIndex = index + declaration.Length;

				if (type.IsGenericTypeDefinition)
				{
					while (nextCharIndex < text.Length && char.IsWhiteSpace(text[nextCharIndex]))
						nextCharIndex++;

					if (nextCharIndex < text.Length && text[nextCharIndex] == '<')
						return true;

					continue;
				}

				if (nextCharIndex >= text.Length || !IsIdentifierChar(text[nextCharIndex]))
					return true;
			}

			return false;
		}

		private static string GetTypeDeclarationKeyword(this Type type)
		{
			if (type.IsInterface)
				return "interface";

			if (type.IsEnum)
				return "enum";

			return type.IsClass ? "class" : "struct";
		}

		private static bool IsIdentifierChar(char c)
		{
			return char.IsLetterOrDigit(c) || c == '_';
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
			_pathToScript?.Clear();
			_typeToScript?.Clear();
			_typeNameToScript?.Clear();

			if (_scriptToTypes == null)
				return;

			foreach (var hashSet in _scriptToTypes.Values)
				hashSet.Clear();
			_scriptToTypes.Clear();
		}
	}
}
