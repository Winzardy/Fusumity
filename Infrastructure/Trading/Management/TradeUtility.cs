using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Content;

namespace Trading
{
	public static partial class TradeUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this ContentEntry<TradeCost> entry, Tradeboard tradeboard, out TradePayError? error)
			=> CanPay(entry.ToReference(), tradeboard, out error);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this in ContentReference<TradeCost> reference, Tradeboard tradeboard, out TradePayError? error)
		{
			tradeboard.Bind(in reference);
			return TradeManager.CanPay(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanPay(this TradeCost cost, Tradeboard tradeboard, string tradeId, out TradePayError? error)
		{
			tradeboard.Bind(tradeId);
			return TradeManager.CanPay(cost, tradeboard, out error);
		}

		/// <inheritdoc cref="TradeUtility.PayAsync(in ContentReference{TradeCost}, Tradeboard, CancellationToken)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PayAsync(this ContentEntry<TradeCost> entry, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			return PayAsync(entry.ToReference(), tradeboard, cancellationToken);
		}

		/// <summary>
		/// Собирает чеки и отправляет их
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradePayError?> PayAsync(this in ContentReference<TradeCost> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference);
			return TradeManager.PayAsync(reference, tradeboard, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanExecute(this in ContentReference<TradeEntry> reference, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.CanExecute(reference, tradeboard, out error);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<TradeExecuteError?> ExecuteAsync(this in ContentReference<TradeEntry> reference, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			tradeboard.Bind(in reference.Read());
			return TradeManager.ExecuteAsync(reference, tradeboard, cancellationToken);
		}
	}
}
