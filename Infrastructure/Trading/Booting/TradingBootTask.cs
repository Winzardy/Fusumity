using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia;
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
			_service = _factory?.Create();
			var management = new TradeManagement(_service);
			TradeManager.Initialize(management);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			//  ReSharper disable once SuspiciousTypeConversion.Global
			if (_service is IDisposable service)
				service.Dispose();

			TradeManager.Terminate();
		}

		public override void OnBootCompleted()
		{
			if (_service is IInitializable service)
				service.Initialize();
		}
	}
}
