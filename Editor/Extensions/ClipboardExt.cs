using System;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Extensions
{
	public static class ClipboardExt
	{
		private static object _source;

		public static void CopyManagedReferenceValue(this SerializedProperty target)
		{
			target.CopyManagedReferenceValue(out _source);
		}

		public static void CopyManagedReferenceValue(this SerializedProperty target, out object source)
		{
#if UNITY_2022_3_OR_NEWER
			if (target.managedReferenceValue == null)
			{
				source = null;
				return;
			}
			source = Activator.CreateInstance(target.managedReferenceValue.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(target.managedReferenceValue, source);
#else
			var type = target.GetManagedReferenceType();
			_source = Activator.CreateInstance(type);
			var sourceObject = target.GetPropertyObjectByLocalPath(target.name);
			EditorUtility.CopySerializedManagedFieldsOnly(sourceObject, _source);
#endif
		}

		public static void PasteManagedReferenceValue(this SerializedProperty target)
		{
			target.PasteManagedReferenceValue(_source);
		}

		public static void PasteManagedReferenceValue(this SerializedProperty target, object source)
		{
			if (source == null)
			{
				target.managedReferenceValue = null;
				return;
			}
			var value = Activator.CreateInstance(source.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(source, value);
			try
			{
				target.managedReferenceValue = value;
			}
			catch
			{
				Debug.LogWarning("Type is mismatched");
			}
		}
	}
}