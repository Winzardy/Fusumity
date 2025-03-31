using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fusumity.Editor.Assistance
{
	public partial class FieldPathExt
	{
		private struct MonoBehaviourProcessor
		{
			public const int headerId = 114;

			private static readonly Regex _guidHeaderRegex = new Regex($@"^\s*(- )?guid:", RegexOptions.Compiled);

			private const string _fieldHeader = "header";
			private static readonly Regex _fieldHeaderRegex = new Regex(@$"^\s*(- )?(?<{_fieldHeader}>[^:]+):", RegexOptions.Compiled);
			private const string _dummy1Field = "dummy1";
			private static readonly Regex _dummy1Regex = new Regex($@"^\s*{_dummy1Field}:\s*(?<{_dummy1Field}>-?\d+)$", RegexOptions.Compiled);
			private const string _dummy2Field = "dummy2";
			private static readonly Regex _dummy2Regex = new Regex($@"^\s*{_dummy2Field}:\s*(?<{_dummy2Field}>-?\d+)$", RegexOptions.Compiled);

			private const string _arrayElementPrefix = "$";

			private bool _isGuidBlock;
			private GuidPath _guidPath;
			private Stack<(int indent, int arrayIndex, bool isArrayBlock, string fieldName)> _path;

			private RidBlockProcessor _ridBlockProcessor;

			public bool TryGetGuidPath(long blockId, Line line, Dictionary<RidKey, RidBlock> ridBlocks, out GuidPath guidPath, out bool repeat)
			{
				var result = false;
				guidPath = default;

				if (!_ridBlockProcessor.TryParseLine(blockId, ref line, ridBlocks, out repeat, out var indentCount, out var skipHeader))
					return false;

				line.GetIndentCount(out var isArrayBlockStart);

				// Достаём информацию о GUID
				if (_isGuidBlock)
				{
					var dummy1Match = _dummy1Regex.Match(line.Text);
					if (dummy1Match.Success)
					{
						_guidPath.guid.dummy1 = long.Parse(dummy1Match.Groups[_dummy1Field].Value);
						_guidPath.dummy1LineIndex = line.lineIndex;
					}
					else
					{
						var dummy2Match = _dummy2Regex.Match(line.Text);
						if (dummy2Match.Success)
						{
							_guidPath.guid.dummy2 = long.Parse(dummy2Match.Groups[_dummy2Field].Value);
							_guidPath.dummy2LineIndex = line.lineIndex;
							result = true;
						}
					}
				}
				// Формируем блоки
				else
				{
					// Если контекст предыдущего блока закончился, то удаляем из стека все элементы предыдущего блока
					var arrayIndex = 0;
					var isArrayBlock = isArrayBlockStart;
					while (_path.TryPeek(out var pathPart) && pathPart.indent >= indentCount)
					{
						_path.Pop();
						if (pathPart.indent == indentCount)
						{
							arrayIndex = pathPart.arrayIndex;
							if (isArrayBlockStart)
								arrayIndex++;
							if (pathPart.isArrayBlock)
								isArrayBlock = true;
						}
					}
					_isGuidBlock = false;

					if (isArrayBlock)
						_path.Push((indentCount, arrayIndex, true, $"{_arrayElementPrefix}{arrayIndex}"));

					if (!skipHeader)
					{
						// Проверяем заголовки на наличие GUID
						var guidHeaderMatch = _guidHeaderRegex.Match(line.Text);
						if (guidHeaderMatch.Success)
							_isGuidBlock = true;
						else
						{
							var headerMatch = _fieldHeaderRegex.Match(line.Text);
							if (headerMatch.Success)
							{
								// Добавляем новый заголовок в стек
								var header = headerMatch.Groups[_fieldHeader].Value;
								_path.Push((indentCount, -1, false, header));
							}
						}
					}
				}

				// Если GUID полностью сформирован, то создаём путь
				if (result)
				{
					_guidPath.path.propertyPath = string.Join(".", _path.Reverse().Skip(1).Select(p => p.fieldName));
					_isGuidBlock = false;
				}

				guidPath = _guidPath;
				return result;
			}

			public void Reset()
			{
				this = new MonoBehaviourProcessor
				{
					_path = _path ??= new (),
					_ridBlockProcessor = _ridBlockProcessor,
				};
				_path.Clear();
				_ridBlockProcessor.Reset();
			}
		}
	}
}
