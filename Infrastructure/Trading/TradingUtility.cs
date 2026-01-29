using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace Trading
{
	public static partial class TradingUtility
	{
		#region Trade

		public static bool CanFetch(TradeConfig trade, Tradeboard tradeboard, out TradeExecuteError? error)
		{
			if (!CanFetch(trade.cost, tradeboard, out var payError))
			{
				error = new TradeExecuteError(payError, null);
				return false;
			}

			if (trade.reward != null)
			{
				if (!trade.reward.CanExecute(tradeboard, out var receiveError))
				{
					error = new TradeExecuteError(payError, receiveError);
					return false;
				}
			}
			else
			{
				// Это как бы допустимо...
				TradingDebug.LogWarning($"Trade by id [ {trade.Id} ] has null reward...");
			}

			error = null;
			return true;
		}

		/// <inheritdoc cref="TradingUtility.FetchAsync(TradeCost, Tradeboard, CancellationToken)"/>
		public static async Task<TradeExecuteError?> FetchAsync(TradeConfig trade, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetch(trade, tradeboard, out var error))
					return error;
			}

			var payError = await FetchAsync(trade.cost, tradeboard, cancellationToken);
			if (payError != null)
				return new TradeExecuteError(payError, null);

			return null;
		}

		#endregion

		public static bool CanFetch([CanBeNull] TradeCost cost, Tradeboard tradeboard, out TradePayError? error)
		{
			if (tradeboard.Id.IsNullOrEmpty())
				throw TradingDebug.Exception("Tradeboard cannot exist without an identifier");

			if (!tradeboard.IsFetchMode)
				throw TradingDebug.Exception("Tradeboard will be fetched!");

			if (cost == null)
			{
				error = null;
				return true;
			}

			foreach (var actualCost in cost.EnumerateActual(tradeboard))
			{
				if (actualCost is IInterceptableTradeCost interceptable)
				{
					if (interceptable.ShouldIntercept(tradeboard))
					{
						error = null;
						return true;
					}
				}

				if (actualCost is ITradeCostWithReceipt tradeCostWithReceipt)
				{
					if (!tradeCostWithReceipt.CanFetch(tradeboard, out error))
						return false;
				}
				else
				{
					if (!actualCost.CanExecute(tradeboard, out error))
						return false;
				}
			}

			error = null;
			return true;
		}

		/// <summary>
		/// Асинхронно подготавливаем оплату либо перехватываем её,
		/// чтобы повлиять на процесс оплаты <br/><br/>
		///
		/// Шаги: <br/>
		/// 1) Переводим контекст сделки (<see cref="Tradeboard"/>) в fetch-режим <br/>
		/// 2) Выполняем клиентские перехваты (<see cref="IInterceptableTradeCost"/>) <br/>
		/// 3) Запрашиваем и собирает торговые чеки <br/>
		/// 4) Регистрируем чеки в контексте сделки (<see cref="Tradeboard"/>) <br/>
		/// 5) Отправляем чеки (<see cref="TradeManager.PushReceipts"/>) <br/>
		/// 6) Проверяем возможность оплаты после отправки чеков (<see cref="TradeManager.CanPay"/>) <br/><br/>
		///
		/// В случае успеха возвращает <c>null</c>, иначе — причину невозможности оплаты
		/// </summary>
		public static async Task<TradePayError?> FetchAsync(TradeCost cost, Tradeboard tradeboard,
			CancellationToken cancellationToken = default)
		{
			if (tradeboard.IsFetchMode)
				throw new ArgumentException("Tradeboard can't be fetched!");

			if (cost == null)
				return null;

			TradePayError? error;
			using (tradeboard.FetchModeScope())
			{
				if (!CanFetch(cost, tradeboard, out error))
					return error;
			}

			using (tradeboard.SimulationModeScope())
			{
				foreach (var actualCost in cost.EnumerateActual(tradeboard))
				{
					if (actualCost is IInterceptableTradeCost interceptable)
					{
						if(!interceptable.ShouldIntercept(tradeboard))
							continue;

						var result = await interceptable.InterceptAsync(tradeboard, cancellationToken);

						if (result == InterceptResult.Cancel)
							return TradePayError.Cancelled;
					}
				}
			}

			using (tradeboard.FetchModeScope())
			{
				using (ListPool<ITradeReceipt>.Get(out var receipts))
				{
					foreach (var actualCost in cost.EnumerateActual(tradeboard))
					{
						try
						{
							if (actualCost is not ITradeCostWithReceipt tradeCostWithReceipt)
								continue;

							if (cancellationToken.IsCancellationRequested)
								return TradePayError.Cancelled;

							var receipt = await tradeCostWithReceipt.FetchAsync(tradeboard, cancellationToken);

							if (cancellationToken.IsCancellationRequested)
								return TradePayError.Cancelled;

							if (receipt != null)
							{
								if (receipt.NeedPush())
									receipts.Add(receipt);
							}
							else
								return TradePayError.NotImplemented; //TODO: ошибка получения чека
						}
						catch (OperationCanceledException)
						{
							return TradePayError.Cancelled;
						}
						catch (Exception e)
						{
							throw TradingDebug.Exception(e.Message);
						}
					}

					if (receipts.Count > 0)
					{
						tradeboard.Register(receipts.ToArray());
						TradeManager.PushReceipts(tradeboard);
					}
				}

				return TradeManager.CanPay(cost, tradeboard, out error) ? null : error;
			}
		}
	}
}
