using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fusumity.Collections;

namespace Fusumity.Editor.Assistance
{
	public static class FieldPathExt
	{
		public struct GuidPath
		{
			public FieldPath path;
			public SerializableGuid guid;
			public int dummy1LineIndex;
			public int dummy2LineIndex;
		}

		public static void SearchGuidPathsInFile(string[] lines, List<GuidPath> buffer)
		{
			buffer.Clear();
			var processor = new BlockProcessor();

			for (var i = 0; i < lines.Length; i++)
			{
				var textLine = new Line
				{
					index = i,
					text = lines[i],
				};
				if (processor.TryGetGuidPath(textLine, out var guidPath))
					buffer.Add(guidPath);
			}
		}

		private struct Line
		{
			public int index;
			public string text;
		}

		private struct BlockProcessor
		{
			public enum BlockType
			{
				Skip,
				MonoBehaviour,
				Prefab,
			}

			public BlockType blockType;

			private int _blockHeaderId;
			private ulong _blockId;

			private const string _blockHeaderIdGroup = "headerId";
			private const string _blockIdGroup = "id";
			private static readonly Regex _headerRegex = new Regex(@$"^--- !u!(?<{_blockHeaderIdGroup}>\d+) &(?<{_blockIdGroup}>\d+)$", RegexOptions.Compiled);

			private MonoBehaviourProcessor _monoBehaviourProcessor;
			private PrefabProcessor _prefabProcessor;

			private bool TryParseHeader(Line line)
			{
				var match = _headerRegex.Match(line.text);
				if (!match.Success)
					return false;

				_blockHeaderId = int.Parse(match.Groups[_blockHeaderIdGroup].Value);
				_blockId = ulong.Parse(match.Groups[_blockIdGroup].Value);

				if (_blockHeaderId == MonoBehaviourProcessor.headerId)
				{
					blockType = BlockType.MonoBehaviour;
					_monoBehaviourProcessor.Reset();
				}
				else if (_blockHeaderId == PrefabProcessor.headerId)
				{
					blockType = BlockType.Prefab;
					_prefabProcessor.Reset();
				}
				else
					blockType = BlockType.Skip;

				return true;
			}

			public bool TryGetGuidPath(Line line, out GuidPath guidPath)
			{
				guidPath = default;
				if (TryParseHeader(line))
					return false;

				switch (blockType)
				{
					case BlockType.MonoBehaviour:
						if (_monoBehaviourProcessor.TryGetGuidPath(line, out guidPath))
						{
							guidPath.path.objectLocalId = _blockId;
							return true;
						}
						return false;
					case BlockType.Prefab:
						if (_prefabProcessor.TryGetGuidPath(line, out guidPath))
						{
							guidPath.path.prefabInstanceId = _blockId;
							return true;
						}
						return false;
					case BlockType.Skip:
					default:
						return false;
				}
			}
		}

		private struct PrefabProcessor
		{
			private enum BlockState
			{
				Nothing,
				ModificationsBlock,
				Skip,
			}

			private enum ModificationType
			{
				Nothing,
				Skip,
				Dummy1,
				Dummy2,
			}

			public const int headerId = 1001;

			private const string _modificationsHeader = "m_Modifications";
			private static readonly Regex _modificationsHeaderRegex = new Regex($@"^\s*{_modificationsHeader}:\s*$", RegexOptions.Compiled);

			private const string _propertyPathGroup = "propertyPath";
			private static readonly Regex _propertyPathDummy1Regex = new Regex($@"^\s*{_propertyPathGroup}: (?<{_propertyPathGroup}>[^\s]+)\.guid\.dummy1$", RegexOptions.Compiled);
			private static readonly Regex _propertyPathDummy2Regex = new Regex($@"^\s*{_propertyPathGroup}: (?<{_propertyPathGroup}>[^\s]+)\.guid\.dummy2$", RegexOptions.Compiled);

			private const string _propertyValueGroup = "value";
			private static readonly Regex _propertyValueRegex = new Regex($@"^\s*{_propertyValueGroup}:\s*(?<{_propertyValueGroup}>-?\d+)$", RegexOptions.Compiled);

			private const string _fileId = "fileID";
			private static readonly Regex _targetRegex = new Regex($@"^\s*- target: \{{{_fileId}: (?<{_fileId}>\d+), guid: [^,]+, type: \d+\}}$", RegexOptions.Compiled);

			private const string _arrayElementPrefix = "$";
			private const string _arrayStart = "Array.data[";
			private const string _arrayEnd = "]";

			private BlockState _blockState;
			private int _modificationsIndentCount;
			private int _indentCount;

			private ModificationType _modificationType;

			private GuidPath _guidPath;

			private string _propertyPath;
			private string _targetFileId;

			public bool TryGetGuidPath(Line line, out GuidPath guidPath)
			{
				guidPath = default;
				switch (_blockState)
				{
					// Ищем блок модификаий префаба (Он может содержать GUID)
					case BlockState.Nothing:
					{
						if (_modificationsHeaderRegex.IsMatch(line.text))
						{
							_blockState = BlockState.ModificationsBlock;
							_modificationsIndentCount = GetIndentCount(line.text);
						}

						return false;
					}
					// Пропускаем строки если блок модификаций уже обработан
					case BlockState.Skip:
						return false;
				}

				_indentCount = GetIndentCount(line.text);

				// Если блок модификаций закончился, то выходим из него
				if (_modificationsIndentCount >= _indentCount)
				{
					_blockState = BlockState.Skip;
					_modificationType = ModificationType.Nothing;
					return false;
				}

				var targetMatch = _targetRegex.Match(line.text);
				if (targetMatch.Success)
				{
					// Запоминаем FileID модификации (Это будет Id компонента в префабе)
					_targetFileId = targetMatch.Groups[_fileId].Value;
					// Это начало блока модификации, сбрасываем тип модификации
					_modificationType = ModificationType.Nothing;
					return false;
				}

				// Если нет модификаций в обработке, то ищем путь к GUID
				if (_modificationType == ModificationType.Nothing)
				{
					var dummy1PathMatch = _propertyPathDummy1Regex.Match(line.text);
					if (dummy1PathMatch.Success)
					{
						_propertyPath = dummy1PathMatch.Groups[_propertyPathGroup].Value;
						_modificationType = ModificationType.Dummy1;
						return false;
					}

					var dummy2PathMatch = _propertyPathDummy2Regex.Match(line.text);
					if (dummy2PathMatch.Success)
					{
						System.Diagnostics.Debug.Assert(_propertyPath == dummy1PathMatch.Groups[_propertyPathGroup].Value);
						_modificationType = ModificationType.Dummy2;
						return false;
					}

					// Если путь не ведёт к GUID, то пропускаем блок
					_modificationType = ModificationType.Skip;
					return false;
				}
				if (_modificationType == ModificationType.Skip)
					return false;

				var propertyValueMatch = _propertyValueRegex.Match(line.text);
				if (propertyValueMatch.Success)
				{
					var value = long.Parse(propertyValueMatch.Groups[_propertyValueGroup].Value);

					if (_modificationType == ModificationType.Dummy1)
					{
						_guidPath.guid.dummy1 = value;
						_guidPath.dummy1LineIndex = line.index;
						_modificationType = ModificationType.Skip;
					}
					else
					{
						_guidPath.guid.dummy2 = value;
						_guidPath.dummy2LineIndex = line.index;
						_modificationType = ModificationType.Skip;

						_guidPath.path.objectLocalId = ulong.Parse(_targetFileId);
						_guidPath.path.propertyPath = _propertyPath.Replace(_arrayStart, _arrayElementPrefix).Replace(_arrayEnd, string.Empty);

						guidPath = _guidPath;
						return true;
					}
				}

				return false;
			}

			private int GetIndentCount(string line)
			{
				var indentCount = 0;
				foreach (var letter in line)
				{
					if (letter is ' ' or '-')
						indentCount++;
					else
						break;
				}

				return indentCount;
			}

			public void Reset()
			{
				this = default;
			}
		}

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
			private Stack<(int indent, int arrayIndex, string fieldName)> _path;

			public bool TryGetGuidPath(Line line, out GuidPath guidPath)
			{
				var result = false;

				var indentCount = GetIndentCount(line.text, out var isArrayElement);

				// Достаём информацио о GUID
				if (_isGuidBlock)
				{
					var dummy1Match = _dummy1Regex.Match(line.text);
					if (dummy1Match.Success)
					{
						_guidPath.guid.dummy1 = long.Parse(dummy1Match.Groups[_dummy1Field].Value);
						_guidPath.dummy1LineIndex = line.index;
					}
					else
					{
						var dummy2Match = _dummy2Regex.Match(line.text);
						if (dummy2Match.Success)
						{
							_guidPath.guid.dummy2 = long.Parse(dummy2Match.Groups[_dummy2Field].Value);
							_guidPath.dummy2LineIndex = line.index;
							result = true;
						}
					}
				}
				// Формируем блоки
				else
				{
					// Если контекст предыдущего блока закончился, то удаляем из стека все элементы предыдущего блока
					var arrayIndex = 0;
					while (_path.TryPeek(out var pathPart) && pathPart.indent >= indentCount)
					{
						_path.Pop();
						if (isArrayElement && pathPart.indent == indentCount)
							arrayIndex = pathPart.arrayIndex + 1;
					}
					_isGuidBlock = false;

					if (isArrayElement)
						_path.Push((indentCount, arrayIndex, $"{_arrayElementPrefix}{arrayIndex}"));

					// Проверяем заголовкии на наличие GUID
					var guidHeaderMatch = _guidHeaderRegex.Match(line.text);
					if (guidHeaderMatch.Success)
						_isGuidBlock = true;
					else
					{
						var headerMatch = _fieldHeaderRegex.Match(line.text);
						if (headerMatch.Success)
						{
							// Добавляем новый заголовок в стек
							var header = headerMatch.Groups[_fieldHeader].Value;
							_path.Push((indentCount, -1, header));
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

			private int GetIndentCount(string line, out bool isArrayElement)
			{
				isArrayElement = false;
				var indentCount = 0;
				foreach (var letter in line)
				{
					if (letter is ' '){}
					else if (letter is '-')
						isArrayElement = true;
					else
						break;

					indentCount++;
				}

				return indentCount;
			}

			public void Reset()
			{
				this = new MonoBehaviourProcessor
				{
					_path = _path ??= new (),
				};
				_path.Clear();
			}
		}
	}
}
