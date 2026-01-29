namespace Trading
{
	/// <summary>
	/// Точка интеграции торгового пайплайна с внешним окружением.
	///
	/// Используется для расширения базовой торговой логики
	/// клиент-специфичными и инфраструктурными операциями
	/// (fetch-режим, перехваты, квитанции и т.п.),
	/// не нарушая доменный контракт торговли
	/// </summary>
	/// <remarks>
	/// Не владеет торговой логикой и не заменяет <see cref="TradeManager"/>,
	/// а дополняет его поведение в зависимости от среды выполнения
	/// (клиент, сервер, тесты).
	/// </remarks>
	public interface ITradeGateway
	{
#if CLIENT
		void PushReceipts(Tradeboard tradeboard);

		bool CanFetch(TradeCost cost, string tradeId, out TradePayError? error);
		bool CanFetch(TradeConfig trade, out TradeExecuteError? error);
#endif
	}
}
