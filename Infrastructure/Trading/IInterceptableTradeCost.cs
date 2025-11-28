using System.Threading;
using System.Threading.Tasks;

namespace Trading
{
	public enum InterceptResult
	{
		/// <summary>
		/// Продолжить оплату
		/// </summary>
		Continue,

		/// <summary>
		/// Отменить оплату
		/// </summary>
		Cancel
	}

	/// <summary>
	/// Позволяет перехватить процесс оплаты перед выполнением TradeCost
	/// </summary>
	/// <remarks>
	/// Только на клиенте
	/// </remarks>
	/// <returns>
	/// Continue — продолжить оплату<br/>
	/// Cancel — отменить оплату
	/// </returns>
	public interface IInterceptableTradeCost
	{
		public bool ShouldIntercept(Tradeboard board);
		public Task<InterceptResult> InterceptAsync(Tradeboard board, CancellationToken cancellationToken);
	}
}
