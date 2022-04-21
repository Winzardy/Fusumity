using System.Reflection;
using Fusumity.Editor.Utilities;
using UnityEditor;
using UnityEngine;

public class SerializationExtensions : MonoBehaviour
{
	public static float GetObjectHeight(object source)
	{
		if (source == null)
			return EditorGUIUtility.singleLineHeight;

		var type = source.GetType();

		var fields = type.GetFields(ReflectionExtensions.fieldBindingFlags);
		var height = GetFieldsHeight(source, fields);

		var baseType = type.BaseType;
		while (baseType != null)
		{
			fields = baseType.GetFields(ReflectionExtensions.internalFieldBindingFlags);
			height += GetFieldsHeight(source, fields);
			baseType = baseType.BaseType;
		}

		return height;
	}

	private static float GetFieldsHeight(object source, FieldInfo[] fields)
	{
		var height = 0f;
		foreach (var field in fields)
		{
			if (field.IsPrivate && field.GetCustomAttribute<SerializeField>() == null ||
			    field.GetCustomAttribute<HideInInspector>() != null)
				continue;

			var value = field.GetValue(source);
			height += GetObjectHeight(value);
		}

		return height;
	}
}
