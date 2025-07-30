using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.App.BootTask;
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
		private ITradingBackendFactory _factory;

		public override int Priority => HIGH_PRIORITY - 150;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var backend = _factory?.Create();
			var management = new TradeManagement(backend);
			TradeManager.Initialize(management);
			return UniTask.CompletedTask;
		}
	}
}
