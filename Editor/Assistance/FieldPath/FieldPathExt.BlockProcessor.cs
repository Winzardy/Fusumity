using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fusumity.Editor.Assistance
{
	public partial class FieldPathExt
	{
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
			private RidBlockProcessor _ridBlockProcessor;

			public bool TryParseHeader(Line line, out ulong blockId)
			{
				blockId = 0;
				var match = _headerRegex.Match(line.Text);
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

				_ridBlockProcessor.Reset();
				blockId = _blockId;
				return true;
			}

			public bool TryGetGuidPath(Line line, Dictionary<RidKey, RidBlock> ridBlocks, out GuidPath guidPath, out bool repeat)
			{
				guidPath = default;
				repeat = false;

				if (TryParseHeader(line, out _) || blockType == BlockType.Skip)
					return false;

				switch (blockType)
				{
					case BlockType.MonoBehaviour:
						if (_monoBehaviourProcessor.TryGetGuidPath(_blockId, line, ridBlocks, out guidPath, out repeat))
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
	}
}
