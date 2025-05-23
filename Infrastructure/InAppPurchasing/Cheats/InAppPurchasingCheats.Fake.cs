using System;
using Cysharp.Threading.Tasks;
using InAppPurchasing.Fake;
using MobileConsole;

namespace InAppPurchasing.Cheats
{
#if DebugLog
	[SettingCommand(name = "System/" + nameof(InAppPurchasing))]
	public class IAPFakeCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeUpdated))]
		public bool useFake;

		private IInAppPurchasingService _main;
		private IInAppPurchasingService _fake;

		public void OnUseFakeUpdated()
		{
			if (!IAPManager.IsInitialized)
				return;

			if (useFake)
			{
				var skip = false;
				if (IAPManager.Service is FakeIAPService service)
				{
					TrySetFake(service);
					skip = true;
				}
				else if (_fake == null)
					TrySetFake(new FakeIAPService());

				if (skip)
					return;

				var prev = IAPManager.SetService(_fake);
				_main ??= prev;
			}
			else if (_main != null)
			{
				IAPManager.SetService(_main);
			}
		}

		private void TrySetFake(FakeIAPService service)
		{
			if (_fake != null)
				return;

			_fake = service;
			LogConsole.GetCommand<IAPFakeRestoreTransactionsCheats>().SetFake((FakeIAPService) _fake);
		}
	}

	[SettingCommand(name = "System/" + nameof(InAppPurchasing))]
	public class IAPFakeRestoreTransactionsCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeRestoreTransactionsUpdated))]
		public bool useFakeRestoreTransactions;

		private FakeIAPService _serivce;

		public void OnUseFakeRestoreTransactionsUpdated()
		{
			if (TryGetIntegration(out var service))
				service.IsRestoreTransactionsSupported = useFakeRestoreTransactions;
		}

		public override void OnVariableValueLoaded() => SetVariablesAsync().Forget();

		public override void InitDefaultVariableValue()
		{
			if (TryGetIntegration(out _serivce))
				useFakeRestoreTransactions = _serivce.IsRestoreTransactionsSupported;
			else
				useFakeRestoreTransactions = FakeIAPService.DEFAULT_USE_FAKE_RESTORE_TRANSACTIONS;
		}

		private bool TryGetIntegration(out FakeIAPService integration)
		{
			integration = _serivce;

			if (_serivce != null)
				return true;
			if (!IAPManager.IsInitialized)
				return false;
			if (IAPManager.Service is not FakeIAPService x)
				return false;

			SetFake(x);
			integration = x;
			return true;
		}

		public void SetFake(FakeIAPService service)
		{
			_serivce = service;
			service.IsRestoreTransactionsSupported = useFakeRestoreTransactions;
			refreshUI?.Invoke();
		}

		private async UniTaskVoid SetVariablesAsync()
		{
			await UniTask.WaitUntil(() => IAPManager.IsInitialized)
			   .Timeout(TimeSpan.FromSeconds(30), DelayType.Realtime);

			if (TryGetIntegration(out var service))
				service.IsRestoreTransactionsSupported = useFakeRestoreTransactions;
		}
	}
#endif
}
