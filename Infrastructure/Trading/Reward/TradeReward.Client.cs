#if CLIENT
using System.Collections.Generic;
using Sapientia.Deterministic;
using UnityEngine;

namespace Trading
{
	public abstract partial class TradeReward
	{
		public static readonly Color COLOR = new(R, G, B, A);
		public const float R = 0.75f;
		public const float G = 0.75f;
		public const float B = 0;
		public const float A = 1;

		protected internal virtual IEnumerable<TradeRewardDrop> OnEnumerateDrop(Tradeboard board, TradeRewardDrop parent)
		{
			yield return new TradeRewardDrop(this, parent);
		}
	}

	public struct TradeRewardDrop
	{
		public TradeReward reward;
		public Fix64 rate;

		public TradeRewardDrop(TradeReward reward) : this(reward, Fix64.One)
		{
		}

		public TradeRewardDrop(TradeReward reward, Fix64 rate)
		{
			this.reward = reward;
			this.rate = rate;
		}

		public TradeRewardDrop(TradeReward reward, TradeRewardDrop parent)
			: this(reward, parent.IsEmpty() ? Fix64.One : parent.rate)
		{
		}
	}

	public static class TradeRewardDropUtility
	{
		public static bool IsEmpty(this in TradeRewardDrop drop) => drop.reward == null;

		public static IEnumerable<TradeRewardDrop> EnumerateDrop(this TradeReward reward, Tradeboard board)
		{
			if (!board.IsSimulationMode)
				throw TradingDebug.Exception(
					$"Drop rates can be calculated only in simulation mode (board: {board.Id}).");

			return reward.OnEnumerateDrop(board, new TradeRewardDrop(reward));
		}

		public static IEnumerable<TradeRewardDrop> UnsafeEnumerateDrop(this TradeReward reward, Tradeboard board, TradeRewardDrop parent)
		{
			return reward.OnEnumerateDrop(board, parent);
		}
	}
}
#endif
