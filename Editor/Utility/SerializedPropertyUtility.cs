using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sapientia.Reflection;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Utility
{
	public static class SerializedPropertyUtility
	{
		/// <summary>
		/// Gets visible children of SerializedProperty at 1 level depth.
		/// </summary>
		/// <param name="serializedProperty">Parent SerializedProperty.</param>
		/// <returns>Collection of SerializedProperty children.</returns>
		public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				yield break;

			var currentProperty = serializedProperty.Copy();
			var nextSiblingProperty = serializedProperty.Copy();
			{
				nextSiblingProperty.NextVisible(false);
			}

			if (currentProperty.NextVisible(true))
			{
				do
				{
					if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
						break;

					yield return currentProperty;
				} while (currentProperty.NextVisible(false));
			}
		}

		/// <summary>
		/// Gets visible children of SerializedProperty at 1 level depth.
		/// </summary>
		/// <param name="serializedProperty">Parent SerializedProperty.</param>
		/// <returns>Collection of SerializedProperty children.</returns>
		public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty serializedProperty)
		{
			if (serializedProperty == null)
				yield break;

			var currentProperty = serializedProperty.Copy();
			var nextSiblingProperty = serializedProperty.Copy();
			{
				nextSiblingProperty.Next(false);
			}

			if (currentProperty.Next(true))
			{
				do
				{
					if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
						break;

					yield return currentProperty;
				} while (currentProperty.Next(false));
			}
		}

		public static void DrawInspector(this SerializedObject serializedObject, params string[] fieldsToSkip)
		{
			serializedObject.Update();
			var property = serializedObject.GetIterator();
			if (property.NextVisible(true))
			{
				do
				{
					if (fieldsToSkip.Any(property.name.Contains))
						continue;

					var serializedProperty = serializedObject.FindProperty(property.name);
					EditorGUILayout.PropertyField(serializedProperty, true);
				} while (property.NextVisible(false));
			}

			serializedObject.ApplyModifiedProperties();
		}

		public static void DrawMonoScriptReference(this SerializedProperty property, string label,
			ref MonoScript reference,
			params GUILayoutOption[] options)
		{
			if (property == null)
				return;

			if (!reference)
				reference = property.type.FindMonoScriptByTypeName();

			if (!reference)
				return;

			var cacheEnabled = GUI.enabled;
			GUI.enabled = false;
			reference = (MonoScript) EditorGUILayout.ObjectField(label, reference, typeof(MonoScript),
				allowSceneObjects: true, options);
			GUI.enabled = cacheEnabled;
		}

		public static void DrawMonoScriptReference(this Type type, string label, ref MonoScript reference,
			params GUILayoutOption[] options)
		{
			if (type == null)
				return;

			if (!reference)
				reference = type.FindMonoScript();

			if (!reference)
				return;

			var cacheEnabled = GUI.enabled;
			GUI.enabled = false;
			reference = (MonoScript) EditorGUILayout.ObjectField(label, reference, typeof(MonoScript),
				allowSceneObjects: true, options);
			GUI.enabled = cacheEnabled;
		}

		public static void DrawMonoScriptReference(this Type type, ref MonoScript reference,
			params GUILayoutOption[] options)
		{
			DrawMonoScriptReference(type, string.Empty, ref reference, options);
		}

		public static void DrawMonoScriptReference(this SerializedProperty property, ref MonoScript reference,
			params GUILayoutOption[] options)
		{
			DrawMonoScriptReference(property, string.Empty, ref reference, options);
		}

		public static SerializedProperty FindParentProperty(this SerializedProperty serializedProperty)
		{
			var propertyPaths = serializedProperty.propertyPath.Split('.');
			if (propertyPaths.Length <= 1)
			{
				return default;
			}

			var parentSerializedProperty = serializedProperty.serializedObject.FindProperty(propertyPaths.First());

			for (var index = 1; index < propertyPaths.Length - 1; index++)
			{
				if (propertyPaths[index] == "Array")
				{
					if (index + 1 == propertyPaths.Length - 1)
					{
						// reached the end
						break;
					}

					if (propertyPaths.Length > index + 1 && Regex.IsMatch(propertyPaths[index + 1], "^data\\[\\d+\\]$"))
					{
						var match = Regex.Match(propertyPaths[index + 1], "^data\\[(\\d+)\\]$");
						var arrayIndex = int.Parse(match.Groups[1].Value);
						parentSerializedProperty = parentSerializedProperty.GetArrayElementAtIndex(arrayIndex);
						index++;
					}
				}
				else
				{
					parentSerializedProperty = parentSerializedProperty.FindPropertyRelative(propertyPaths[index]);
				}
			}

			return parentSerializedProperty;
		}

		public static object GetValueByReflection(this SerializedProperty property)
		{
			object obj = property.serializedObject.targetObject;
			var path = property.propertyPath
				.Replace(".Array.data[", "[")
				.Split('.');

			foreach (var part in path)
			{
				if (part.Contains("["))
				{
					var fieldName = part[..part.IndexOf('[')];
					var index = int.Parse(part[(part.IndexOf('[') + 1)..^1]);

					var list = obj.GetValueByReflectionSafe(fieldName) as IList;
					obj = list![index];
				}
				else
				{
					obj = obj.GetValueByReflectionSafe(part);
				}

				if (obj == null)
					break;
			}

			return obj;
		}

		public static object GetValueByReflectionSafe(this SerializedProperty property)
		{
			object obj = property.serializedObject.targetObject;
			var path = property.propertyPath
				.Replace(".Array.data[", "[")
				.Split('.');

			foreach (var part in path)
			{
				if (part.Contains("["))
				{
					var fieldName = part[..part.IndexOf('[')];
					var index = int.Parse(part[(part.IndexOf('[') + 1)..^1]);

					if (obj.GetValueByReflectionSafe(fieldName) is not IList list)
						return null;

					obj = list![index];
				}
				else
				{
					obj = obj.GetValueByReflectionSafe(part);
				}

				if (obj == null)
					break;
			}

			return obj;
		}
	}
}
