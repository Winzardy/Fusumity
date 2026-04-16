using Sapientia.Extensions;
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

		public static string[] GetDirectoriesSafe(string path)
		{
			return
				Directory.Exists(path) ?
				Directory.GetDirectories(path) :
				null;
		}

		public static void CopyDirectory(string sourceDirectory, string destDirectory, bool copySubDirectories = false, bool overwriteFiles = false)
		{
			var dir = new DirectoryInfo(sourceDirectory);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirectory);
			}

			var dirs = dir.GetDirectories();

			Directory.CreateDirectory(destDirectory);
			foreach (var file in dir.GetFiles())
			{
				var tempPath = Path.Combine(destDirectory, file.Name);
				file.CopyTo(tempPath, overwriteFiles);
			}

			if (copySubDirectories)
			{
				foreach (var subdir in dirs)
				{
					var tempPath = Path.Combine(destDirectory, subdir.Name);
					CopyDirectory(subdir.FullName, tempPath, copySubDirectories, overwriteFiles);
				}
			}
		}

		public static string GetFilePathWithoutExtension(string filePath)
		{
			if(filePath.IsNullOrEmpty())
				return null;

			return Path.Combine(
				Path.GetDirectoryName(filePath),
				Path.GetFileNameWithoutExtension(filePath));
		}

		public static void EnsureDirectoryExists(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
		}

		public static void EnsureDirectoryForFileExists(string filePath)
		{
			var directoryPath = Path.GetDirectoryName(filePath);

			if (!directoryPath.IsNullOrEmpty())
				EnsureDirectoryExists(directoryPath);
		}

		public static bool IsPathWithinDirectory(string path, string directory)
		{
			var fullPath = Path.GetFullPath(path);
			var fullDir = Path.GetFullPath(directory)
				.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

			return fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Serializable]
	public struct FolderPath
	{
		public string path;
		public static implicit operator string(FolderPath folderPath) => folderPath.path;
		public static implicit operator FolderPath(string path) => new() { path = path };
	}
}
