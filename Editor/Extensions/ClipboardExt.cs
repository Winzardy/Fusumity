using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class ClipboardExt
	{
		private static object _source;

		public static void CopyPasteValue<T>(this T source, ref T target)
			where T: class
		{
			CopyValue(source, out var boxedSource);
			PasteValue(ref target, boxedSource);
		}

		public static void CopyPasteValueAs<T, T1>(this T source, ref T1 result)
			where T: class
			where T1: class
		{
			var clone = UnsafeUtility.As<T, T1>(ref source);
			clone.CopyPasteValue(ref result);
		}

		public static void CopyValue(this SerializedProperty target)
		{
			target.CopyValue(out _source);
		}

		public static void CopyValue(this SerializedProperty target, out object source)
		{
			CopyValue(target.boxedValue, out source);
		}

		public static void CopyValue<T>(this T value, out object source)
		{
			if (value == null)
			{
				source = null;
				return;
			}

			source = CreateInstance(value.GetType());

			EditorUtility.CopySerializedManagedFieldsOnly(value, source);
		}

		public static void PasteValue(this SerializedProperty target)
		{
			target.PasteValue(_source);
		}

		public static void PasteValue(this SerializedProperty target, object source)
		{
			if (source == null)
			{
				target.boxedValue = null;
				return;
			}
			var value = CreateInstance(source.GetType());

			EditorUtility.CopySerializedManagedFieldsOnly(source, value);
			try
			{
				target.boxedValue = value;
			}
			catch
			{
				Debug.LogWarning("Type is mismatched");
			}
		}

		public static void PasteValue<T>(ref T value, object source)
			where T: class
		{
			if (source == null)
			{
				value = null;
				return;
			}
			value = CreateInstance<T>();
			try
			{
				EditorUtility.CopySerializedManagedFieldsOnly(source, value);
			}
			catch
			{
				Debug.LogWarning("Type is mismatched");
			}
		}

		private static T CreateInstance<T>()
		{
			var valueType = typeof(T);
			if (valueType.IsArray)
				return (T)(object)Array.CreateInstance(valueType.GetElementType()!, 0);
			else
				return Activator.CreateInstance<T>();
		}

		private static object CreateInstance(Type valueType)
		{
			if (valueType.IsArray)
				return Array.CreateInstance(valueType.GetElementType()!, 0);
			else
				return Activator.CreateInstance(valueType);
		}
	}
}
