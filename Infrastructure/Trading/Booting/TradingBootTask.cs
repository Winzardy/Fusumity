using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Trading;
using UnityEngine;

namespace Booting.Trading
{
	[TypeRegistryItem(
		"\u2009Trading", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Cart4)]
	[Serializable]
	public class TradingBootTask : BaseBootTask
	{
		[LabelText("Backend")]
		[SerializeReference]
		private ITradingServiceFactory _factory;

		private ITradingService _service;

		public override int Priority => HIGH_PRIORITY - 150;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var service = _factory?.Create();
			var management = new TradeManagement(service);
			TradeManager.Initialize(management);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			//  ReSharper disable once SuspiciousTypeConversion.Global
			if (_service is IDisposable disposable)
				disposable.Dispose();
		}
	}
}
