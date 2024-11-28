using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class ClipboardExt
	{
		private static object _source;

		public static void CopyPasteValue<T>(this T source, ref T target)
		{
			CopyValue(source, out var boxedSource);
			PasteValue(ref target, boxedSource);
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

			source = Activator.CreateInstance(value.GetType());
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
			var value = Activator.CreateInstance(source.GetType());
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
		{
			if (source == null)
			{
				value = default;
				return;
			}
			value = Activator.CreateInstance<T>();
			try
			{
				EditorUtility.CopySerializedManagedFieldsOnly(source, value);
			}
			catch
			{
				Debug.LogWarning("Type is mismatched");
			}
		}
	}
}
