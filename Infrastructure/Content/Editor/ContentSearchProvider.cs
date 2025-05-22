using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Content.Editor
{
	internal static class ContentSearchProvider
	{
		private const string FILTER = "content:";
		private const string FILTER_GUID = "guid:";
		private const string FILTER_REF = "ref=";

		[SearchItemProvider]
		internal static SearchProvider CreateProvider()
		{
			return new SearchProvider("content", "Content")
			{
				filterId = $"{FILTER}",
				priority = 1,
				showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
				fetchItems = (context, _, provider) => FetchItems(context, provider),
				fetchThumbnail = (item, _) => AssetDatabase.GetCachedIcon(item.description) as Texture2D,
				fetchPreview = (item, _, _, _) => AssetDatabase.GetCachedIcon(item.description) as Texture2D,
				fetchLabel = (item, _) => AssetDatabase.LoadMainAssetAtPath(item.description).name,
				fetchDescription = (item, _) => AssetDatabase.LoadMainAssetAtPath(item.description).name,
				toObject = (item, _) => AssetDatabase.LoadMainAssetAtPath(item.description),
				trackSelection = TrackSelection
			};
		}

		private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
		{
			var query = context.searchQuery.Trim();

			var guidStr = query;
			if (query.StartsWith(FILTER))
			{
				guidStr = query
				   .Remove(FILTER)
				   .Trim();
			}
			else if (query.StartsWith(FILTER_GUID))
			{
				guidStr = query
				   .Remove(FILTER_GUID)
				   .Trim();
			}
			else if (query.StartsWith(FILTER_REF))
			{
				var assetPath = query[5..].TrimEnd('"');
				guidStr = AssetDatabase.AssetPathToGUID(assetPath);
			}

			if (!TryParse(guidStr, out var low, out var high))
				yield break;

			var regex = new Regex($"guid:\\s+low:\\s+{low}\\s+high:\\s+{high}", RegexOptions.Compiled);

			var paths = AssetDatabase.GetAllAssetPaths()
			   .Where(p =>
					p.EndsWith(".asset") ||
					p.EndsWith(".prefab") ||
					p.EndsWith(".unity")
				);

			foreach (var path in paths)
			{
				string yaml;
				try
				{
					yaml = File.ReadAllText(path);
				}
				catch
				{
					continue;
				}

				var matches = regex.Matches(yaml);
				if (matches.Count == 0)
					continue;

				var asset = AssetDatabase.LoadMainAssetAtPath(path);
				if (!asset || asset.ToGuid() == guidStr)
					continue;

				foreach (Match match in matches)
				{
					var index = match.Index;
					var preceding = yaml.Substring(0, index);

					var fieldName = ExtractYamlFieldName(preceding);
					var isAsset = path.EndsWith(".asset");
					var isEntry = isAsset && preceding.LastIndexOf("_serializeReference:") > preceding.LastIndexOf("guid:");

					var label = isEntry
						? $"📄 Content Entry in: {Path.GetFileName(path)} ({fieldName})"
						: $"🔗 Content Reference in: {Path.GetFileName(path)} ({fieldName})";

					yield return provider.CreateItem(
						context,
						id: $"{(isEntry ? "entry" : "ref")}_{path}_{index}",
						label: label,
						description: path,
						thumbnail: AssetPreview.GetMiniThumbnail(asset),
						data: asset
					);
				}
			}
		}

		private static string ExtractYamlFieldName(string preceding)
		{
			return preceding.Split('\n')
			   .Reverse()
			   .Select(l => l.Trim())
			   .FirstOrDefault(l => l.EndsWith(":") && !l.StartsWith("guid") && !l.StartsWith("_serializeReference"))
			  ?.Replace(":", "").Trim() ?? "unknown";
		}

		private static bool TryParse(string input, out string low, out string high)
		{
			low = high = null;

			if (SerializableGuid.TryParse(input, out var guid))
			{
				low = guid.low.ToString();
				high = guid.high.ToString();
				return true;
			}

			var match = Regex.Match(input, @"low:(\-?\d+)\s+high:(\-?\d+)");
			if (match.Success)
			{
				low = match.Groups[1].Value;
				high = match.Groups[2].Value;
				return true;
			}

			return false;
		}

		private static void TrackSelection(SearchItem searchItem, SearchContext _)
		{
			var assetAtPath = AssetDatabase.LoadMainAssetAtPath(searchItem.description);
			EditorGUIUtility.PingObject(assetAtPath);
		}
	}
}
