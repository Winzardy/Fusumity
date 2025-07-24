using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class IOUtility
	{
		public static string GetRelativePath(this string absoulteFilePath)
		{
			absoulteFilePath = Path.GetFullPath(absoulteFilePath);
			string absoulteAssetsDirectory = Path.GetFullPath(Application.dataPath);
			string relativeFilePath = absoulteFilePath.Replace(absoulteAssetsDirectory, "Assets");
			return relativeFilePath;
		}

		public static string GetAbsolutePath(this string relativeAssetPath)
		{
			if (relativeAssetPath.StartsWith("Assets"))
				return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeAssetPath));

			return Path.Combine(Application.dataPath, relativeAssetPath);
		}

		public static string[] GetFiles(this string directory, params string[] extensions)
		{
			var exts = new HashSet<string>(extensions);
			return Directory
			   .EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
			   .Where(s => exts.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())).ToArray();
		}
	}

	[Serializable]
	public struct FolderPath
	{
		public string path;
		public static implicit operator string(FolderPath folderPath) => folderPath.path;
		public static implicit operator FolderPath(string path) => new() {path = path};
	}
}
