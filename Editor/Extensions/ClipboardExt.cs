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
			if (target.managedReferenceValue == null)
			{
				_source = null;
				return;
			}
			_source = Activator.CreateInstance(target.managedReferenceValue.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(target.managedReferenceValue, _source);
		}

		public static void PasteManagedReferenceValue(this SerializedProperty target)
		{
			if (_source == null)
			{
				target.managedReferenceValue = null;
				return;
			}
			var value = Activator.CreateInstance(_source.GetType());
			EditorUtility.CopySerializedManagedFieldsOnly(_source, value);
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