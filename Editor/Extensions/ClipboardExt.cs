using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class ClipboardExt
	{
		private static object _source;

		public static void CopyValue(this SerializedProperty target)
		{
			target.CopyValue(out _source);
		}

		public static void CopyValue(this SerializedProperty target, out object source)
		{
#if UNITY_2022_3_OR_NEWER
			if (target.managedReferenceValue == null)
			{
				source = null;
				return;
			}

			source = Activator.CreateInstance(target.boxedValue.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(target.boxedValue, source);
#else
			var type = target.GetManagedReferenceType();
			_source = Activator.CreateInstance(type);
			var sourceObject = target.GetPropertyObjectByLocalPath(target.name);
			EditorUtility.CopySerializedManagedFieldsOnly(sourceObject, _source);
#endif
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
	}
}