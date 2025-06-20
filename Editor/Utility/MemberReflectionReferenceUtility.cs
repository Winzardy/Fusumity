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
			var rawMembers = path.Replace(".Array.data", string.Empty).Split(".").Skip(skipSteps);

			using (ListPool<MemberReferencePathStep>.Get(out var steps))
			{
				foreach (var raw in rawMembers)
				{
					if (raw.Contains("["))
					{
						var name = raw[..raw.IndexOf("[", StringComparison.Ordinal)];
						var indexStr = raw[(raw.IndexOf("[", StringComparison.Ordinal) + 1)..].TrimEnd(']');

						if (!int.TryParse(indexStr, out var index))
							throw new Exception("Could not parse index");

						steps.Add((name, index));
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
