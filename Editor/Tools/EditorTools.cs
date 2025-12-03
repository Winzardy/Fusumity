using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Fusumity.Editor
{
	public static class EditorTools
	{
		public static void ClearConsole()
		{
			var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
			logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
		}

		public static string GetSelectedFolderPath()
		{
			UnityEngine.Object[] objs = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
			var selected = objs.FirstOrDefault();

			if (selected != null)
			{
				return AssetDatabaseUtility.GetFolderPath(selected);
			}
			else
			{
				return GetActiveFolderPath();
			}
		}

		public static string GetActiveFolderPath()
		{
			return (string)typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
		}

		public static void PingObject(string filePath)
		{
			if (filePath.IsNullOrEmpty())
				return;

			UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
			PingObject(asset);
		}

		public static void PingObject(UnityEngine.Object asset)
		{
			if (asset != null)
			{
				Selection.activeObject = asset;
				EditorGUIUtility.PingObject(asset);
			}
		}
	}
}
