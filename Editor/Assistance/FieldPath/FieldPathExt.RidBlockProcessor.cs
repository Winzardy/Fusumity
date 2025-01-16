using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fusumity.Editor.Assistance
{
	public partial class FieldPathExt
	{
		private struct RidKey : IEquatable<RidKey>
		{
			public ulong blockId;
			public long rid;

			public bool Equals(RidKey other)
			{
				return blockId == other.blockId && rid == other.rid;
			}

			public override bool Equals(object obj)
			{
				return obj is RidKey other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(blockId, rid);
			}
		}

		private struct RidBlock
		{
			public int dataLineIndex;
			public int length;
		}

		private struct RidBlockProcessor
		{
			// Формат references блока:
			//  references:
			//    version: 2
			//    RefIds:
			// references поле всегда находится в корне main блока
			private const string _referencesBlockHeader = "  references:";

			// Формат Rid блока:
			// - rid: 303242092361811255
			//	 type: {class: 'AffectAngle`1[[GameLogic.World.SystemGroups.Stats.SpellStatType, Stats]]', ns: GameLogic.World.SystemGroups.Stats, asm: Stats}
			//	 data:
			private const string _ridField = "rid";
			private static readonly Regex _ridRegex = new Regex($@"^\s*(- )?{_ridField}:\s*(?<{_ridField}>-?\d+)$", RegexOptions.Compiled);
			private static readonly Regex _dataRegex = new Regex($@"^\s*data:", RegexOptions.Compiled);

			private int _referencesIndent;
			private List<(int currentIndex, int additionalIndent, RidBlock ridBlock)> _currentRidBlocks;

			public bool TryParseLine(ulong blockId, ref Line line, Dictionary<RidKey, RidBlock> ridBlocks,
				out bool repeat, out int indentCount, out bool skipHeader)
			{
				repeat = false;

				var baseIndentCount = line.GetIndentCount(out _);
				indentCount = baseIndentCount;
				skipHeader = false;

				// Пропускаем весь references блок
				if (_referencesIndent > 0)
				{
					if (_referencesIndent < baseIndentCount)
						return false;
					// references блок закончился
					_referencesIndent = -1;
					return true;
				}
				// references поле всегда находится в корне main блока
				if (_referencesBlockHeader == line.Text)
				{
					_referencesIndent = baseIndentCount;
					return false;
				}

				// Обрабатываем Rid блоки (Они могут включать друг друга, поэтому используем стек)
				if (_currentRidBlocks.Count > 0)
				{
					var currentRidBlockData = _currentRidBlocks[^1];

					// Пока Rid блок не закончился - обрабатываем строки Rid блока
					if (currentRidBlockData.ridBlock.length > currentRidBlockData.currentIndex)
					{
						// Обрабатываем Rid блок
						line.lineIndex = currentRidBlockData.ridBlock.dataLineIndex + currentRidBlockData.currentIndex;
						indentCount = baseIndentCount + currentRidBlockData.additionalIndent;

						indentCount += line.GetIndentCount(out _) - baseIndentCount;

						currentRidBlockData.currentIndex++;
						_currentRidBlocks[^1] = currentRidBlockData;

						repeat = true;
					}
					else
					{
						_currentRidBlocks.RemoveAt(_currentRidBlocks.Count - 1);
						repeat = _currentRidBlocks.Count > 0;
						return false;
					}
				}

				// Пытаемся найти Rid блок
				var ridMatch = _ridRegex.Match(line.Text);
				if (ridMatch.Success)
				{
					var rid = long.Parse(ridMatch.Groups[_ridField].Value);
					var ridKey = new RidKey
					{
						blockId = blockId,
						rid = rid,
					};

					// Обрабатываем только 1ю ссылку на Rid блок
					// Если существует больше 1й ссылки - остальные игнорируем
					if (ridBlocks.Remove(ridKey, out var ridBlock))
					{
						// Формат Rid блока:
						// - rid: 303242092361811255
						//	 type: {class: 'AffectAngle`1[[GameLogic.World.SystemGroups.Stats.SpellStatType, Stats]]', ns: GameLogic.World.SystemGroups.Stats, asm: Stats}
						//	 data:
						// ridBlock.dataLineIndex - это строка "data:"
						// Поэтому мы сразу переходим на строку с данными.
						// ridBlock.length включает в себя строку "data:", поэтому если данных нет - сразу выходим.
						if (ridBlock.length > 0)
						{
							var blockIndent = line.GetIndentCount(out _) - baseIndentCount;
							if (_currentRidBlocks.Count > 0)
								blockIndent += _currentRidBlocks[^1].additionalIndent;

							_currentRidBlocks.Add((0, blockIndent, ridBlock));
							repeat = true;
							skipHeader = true;

							indentCount = baseIndentCount + blockIndent;
							return true;
						}
					}

					return false;
				}

				return true;
			}

			public void Reset()
			{
				this = new RidBlockProcessor()
				{
					_currentRidBlocks = _currentRidBlocks ??= new (),
				};
				_referencesIndent = -1;
				_currentRidBlocks.Clear();
			}

			public static Dictionary<RidKey, RidBlock> SearchRidBlocks(string[] lines)
			{
				var result = new Dictionary<RidKey, RidBlock>();

				var blockProcessor = new BlockProcessor();

				var shouldSkip = true;
				var isRefIdsBlock = false;

				var refIdsIndentCount = 0;
				var ridIndentCount = 0;

				var currentRidKey = new RidKey();
				var currentRidBlock = new RidBlock
				{
					dataLineIndex = -1,
				};

				for (var i = 0; i < lines.Length; i++)
				{
					var line = new Line
					{
						lineIndex = i,
						sourceLines = lines,
					};

					// Начат новый main блок
					if (blockProcessor.TryParseHeader(line, out var blockId))
					{
						currentRidKey.blockId = blockId;
						shouldSkip = false;
						isRefIdsBlock = false;
						ridIndentCount = -1;
						continue;
					}

					// Блок RefIds закончился, то пропускаем всё до следующего главного блока
					if (shouldSkip)
						continue;

					if (!isRefIdsBlock)
					{
						// Формат references блока:
						//  references:
						//    version: 2
						//    RefIds:
						// references поле всегда находится в корне main блока
						if (_referencesBlockHeader != line.Text)
							continue;

						isRefIdsBlock = true;
						refIdsIndentCount = line.GetIndentCount(out _);

						i += 2; // Пропускаем 2 строки с "version: 2" и "RefIds"
						continue;
					}

					var indentCount = line.GetIndentCount(out var isArrayElement);

					if (ridIndentCount != -1)
					{
						// Если Rid блок обрабатывается, то пропускаем строки
						if (ridIndentCount < indentCount)
							continue;

						// Если блок Rid закончился, то сохраняем его
						// Убираем "data:"
						currentRidBlock.dataLineIndex += 1;
						currentRidBlock.length = i - currentRidBlock.dataLineIndex;
						if (currentRidBlock.length > 0)
							result.Add(currentRidKey, currentRidBlock);
					}

					ridIndentCount = -1;
					// Проверяем на начало Rid блока
					var ridMatch = _ridRegex.Match(line.Text);
					if (ridMatch.Success)
					{
						currentRidKey.rid = long.Parse(ridMatch.Groups[_ridField].Value);
						// Формат Rid блока:
						// - rid: 303242092361811255
						//	 type: {class: 'AffectAngle`1[[GameLogic.World.SystemGroups.Stats.SpellStatType, Stats]]', ns: GameLogic.World.SystemGroups.Stats, asm: Stats}
						//	 data:
						// Блок "data:" может отсутствовать, в таком случаем пропускаем блок
						if (!line.TrySetNextLine(2) || !_dataRegex.IsMatch(line.Text))
							continue;

						// Сразу переходим на строку "data:"
						i = line.lineIndex;
						currentRidBlock.dataLineIndex = i;

						ridIndentCount = indentCount;
						continue;
					}

					// Если блок RefIds закончился, то пропускаем всё до следующего main блока
					if (refIdsIndentCount >= indentCount)
					{
						shouldSkip = true;
						isRefIdsBlock = false;
						refIdsIndentCount = -1;
					}
				}

				return result;
			}
		}
	}
}
