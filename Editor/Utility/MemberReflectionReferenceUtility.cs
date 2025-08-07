using System;
using System.Linq;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Fusumity.Editor.Utility
{
	public static class MemberReflectionReferenceUtility
	{
		private const string ARRAY_REMOVE = ".Array.data";
		private const string ARRAY_START_SYMBOL = "[";
		private const char ARRAY_END_SYMBOL = ']';

		private const string DICTIONARY_REPLACE = ".{";
		private const string DICTIONARY_START_SYMBOL = "{";
		private const char DICTIONARY_END_SYMBOL = '}';

		private const string SEPARATOR = ".";

		/// <param name="skipSteps">Сколько шагов нужно пропустить в начале</param>
		public static MemberReflectionReference<T> ToReference<T>(this InspectorProperty property, int skipSteps = 0)
		{
			var propertyPath = property.UnityPropertyPath;

			if (propertyPath.Contains("{temp"))
				propertyPath = property.Tree.UnitySerializedObject
				   .FindProperty(property.Path)?.propertyPath;

			if (propertyPath.IsNullOrEmpty())
				throw new Exception("Property path is null or empty");

			return ToReference<T>(propertyPath, skipSteps);
		}

		/// <param name="skipSteps">Сколько шагов нужно пропустить в начале</param>
		public static MemberReflectionReference<T> ToReference<T>(this SerializedProperty property, int skipSteps = 0)
			=> ToReference<T>(property.propertyPath, skipSteps);

		private static MemberReflectionReference<T> ToReference<T>(string path, int skipSteps)
		{
			var rawMembers = path
			   .Replace(ARRAY_REMOVE, string.Empty)
			   .Replace(DICTIONARY_REPLACE, DICTIONARY_START_SYMBOL)
			   .Split(SEPARATOR)
			   .Skip(skipSteps);

			using (ListPool<MemberReferencePathStep>.Get(out var steps))
			{
				foreach (var raw in rawMembers)
				{
					if (raw.Contains(ARRAY_START_SYMBOL))
					{
						var name = raw[..raw.IndexOf(ARRAY_START_SYMBOL, StringComparison.Ordinal)];
						var indexStr = raw[(raw.IndexOf(ARRAY_START_SYMBOL, StringComparison.Ordinal) + 1)..].TrimEnd(ARRAY_END_SYMBOL);

						if (!int.TryParse(indexStr, out var index))
							throw new Exception("Could not parse index");

						steps.Add((name, index));
					}
					else if (raw.Contains(DICTIONARY_START_SYMBOL))
					{
						var name = raw[..raw.IndexOf(DICTIONARY_START_SYMBOL, StringComparison.Ordinal)];
						var key = raw[(raw.IndexOf(DICTIONARY_START_SYMBOL, StringComparison.Ordinal) + 1)..]
						   .TrimEnd(DICTIONARY_END_SYMBOL);

						if (!key.Contains("\""))
							key = key[..^2];
						else
							key = key[1..^1];

						steps.Add((name, key));
					}
					else
					{
						steps.Add(raw);
					}
				}

				return steps.ToArray();
			}
		}
	}
}
