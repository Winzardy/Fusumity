using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fusumity.Editor.Assistance
{
	public partial class FieldPathExt
	{
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
						if (_modificationsHeaderRegex.IsMatch(line.Text))
						{
							_blockState = BlockState.ModificationsBlock;
							_modificationsIndentCount = line.GetIndentCount(out _);
						}

						return false;
					}
					// Пропускаем строки если блок модификаций уже обработан
					case BlockState.Skip:
						return false;
				}

				_indentCount = line.GetIndentCount(out _);

				// Если блок модификаций закончился, то выходим из него
				if (_modificationsIndentCount >= _indentCount)
				{
					_blockState = BlockState.Skip;
					_modificationType = ModificationType.Nothing;
					return false;
				}

				var targetMatch = _targetRegex.Match(line.Text);
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
					var dummy1PathMatch = _propertyPathDummy1Regex.Match(line.Text);
					if (dummy1PathMatch.Success)
					{
						_propertyPath = dummy1PathMatch.Groups[_propertyPathGroup].Value;
						_modificationType = ModificationType.Dummy1;
						return false;
					}

					var dummy2PathMatch = _propertyPathDummy2Regex.Match(line.Text);
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

				var propertyValueMatch = _propertyValueRegex.Match(line.Text);
				if (propertyValueMatch.Success)
				{
					var value = long.Parse(propertyValueMatch.Groups[_propertyValueGroup].Value);

					if (_modificationType == ModificationType.Dummy1)
					{
						_guidPath.guid.dummy1 = value;
						_guidPath.dummy1LineIndex = line.lineIndex;
						_modificationType = ModificationType.Skip;
					}
					else
					{
						_guidPath.guid.dummy2 = value;
						_guidPath.dummy2LineIndex = line.lineIndex;
						_modificationType = ModificationType.Skip;

						_guidPath.path.objectLocalId = ulong.Parse(_targetFileId);
						_guidPath.path.propertyPath = _propertyPath.Replace(_arrayStart, _arrayElementPrefix).Replace(_arrayEnd, string.Empty);

						guidPath = _guidPath;
						return true;
					}
				}

				return false;
			}

			public void Reset()
			{
				this = default;
			}
		}
	}
}
