using System.Collections.Generic;
using Fusumity.Collections;

namespace Fusumity.Editor.Assistance
{
	public partial class FieldPathExt
	{
		public struct GuidPath
		{
			public FieldPath path;
			public SerializableGuid guid;
			public int dummy1LineIndex;
			public int dummy2LineIndex;
		}

		public static void SearchGuidPathsInFile(string assetGuid, string[] lines, List<GuidPath> buffer)
		{
			buffer.Clear();
			var processor = new BlockProcessor();
			var ridBlocks = RidBlockProcessor.SearchRidBlocks(lines);

			for (var i = 0; i < lines.Length; i++)
			{
				var textLine = new Line
				{
					lineIndex = i,
					sourceLines = lines,
				};

				var repeat = false;
				do
				{
					if (processor.TryGetGuidPath(textLine, ridBlocks, out var guidPath, out repeat))
					{
						guidPath.path.assetGuid = assetGuid;
						buffer.Add(guidPath);
					}
				} while (repeat);
			}
		}

		private struct Line
		{
			public int lineIndex;
			public string[] sourceLines;

			public string Text => sourceLines[lineIndex];

			public bool TrySetNextLine(int count = 1)
			{
				if (sourceLines.Length <= lineIndex + count)
					return false;
				lineIndex += count;
				return true;
			}

			public int GetIndentCount(out bool isArrayBlockStrat)
			{
				isArrayBlockStrat = false;
				var indentCount = 0;
				foreach (var letter in sourceLines[lineIndex])
				{
					if (letter is ' '){}
					else if (letter is '-')
						isArrayBlockStrat = true;
					else
						break;

					indentCount++;
				}

				return indentCount;
			}
		}
	}
}
