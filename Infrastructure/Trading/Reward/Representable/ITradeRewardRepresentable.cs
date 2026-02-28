#if CLIENT
using Sapientia;
using Sapientia.Evaluators;

namespace Trading
{
	public interface ITradeRewardRepresentable
	{
		// Для некоторый кейсов (юридически) недопустимо скрывать список наград за фасадом)
		public const string VISUAL_IGNORE_FLAG = "representable_visual_ignore";

		string VisualId { get; }

		public static bool IsVisualIgnore(Tradeboard board)
		{
			if(board.TryGet<bool>(VISUAL_IGNORE_FLAG, out var ignore))
				return ignore;
			return false;
		}
	}

	public interface ITradeRewardRepresentableWithCount : ITradeRewardRepresentable
	{
		public ref readonly EvaluatedValue<Blackboard,int> Count { get; }
	}
}
#endif
