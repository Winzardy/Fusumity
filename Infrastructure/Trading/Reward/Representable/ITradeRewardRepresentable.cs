using Sapientia;
using Sapientia.Evaluators;

#if CLIENT
namespace Trading
{
	public interface ITradeRewardRepresentable
	{
		string VisualId { get; }
	}

	public interface ITradeRewardRepresentableWithCount : ITradeRewardRepresentable
	{
		public ref readonly EvaluatedValue<Blackboard,int> Count { get; }
	}
}
#endif
