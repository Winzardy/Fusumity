#if DebugLog
using System;
using Cysharp.Threading.Tasks;
using InAppPurchasing.Fake;
using MobileConsole;

namespace InAppPurchasing.Cheats
{
	[SettingCommand(name = InAppPurchasingCheatsUtility.SETTINGS_PATH)]
	public class IAPFakeCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeUpdated))]
		public bool useFake;

		private IInAppPurchasingIntegration _main;
		private IInAppPurchasingIntegration _fake;

		public void OnUseFakeUpdated()
		{
			if (!IAPManager.IsInitialized)
				return;

			if (useFake)
			{
				var skip = false;
				if (IAPManager.Integration is FakeIAPIntegration service)
				{
					TrySetFake(service);
					skip = true;
				}
				else if (_fake == null)
					TrySetFake(new FakeIAPIntegration());

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

		private void TrySetFake(FakeIAPIntegration integration)
		{
			if (_fake != null)
				return;

			_fake = integration;
			LogConsole.GetCommand<IAPFakeRestoreTransactionsCheats>().SetFake((FakeIAPIntegration) _fake);
		}
	}

	[SettingCommand(name = InAppPurchasingCheatsUtility.SETTINGS_PATH)]
	public class IAPFakeRestoreTransactionsCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeRestoreTransactionsUpdated))]
		public bool useFakeRestoreTransactions;

		private FakeIAPIntegration _serivce;

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
				useFakeRestoreTransactions = FakeIAPIntegration.DEFAULT_USE_FAKE_RESTORE_TRANSACTIONS;
		}

		private bool TryGetIntegration(out FakeIAPIntegration integration)
		{
			integration = _serivce;

			if (_serivce != null)
				return true;
			if (!IAPManager.IsInitialized)
				return false;
			if (IAPManager.Integration is not FakeIAPIntegration x)
				return false;

			SetFake(x);
			integration = x;
			return true;
		}

		public void SetFake(FakeIAPIntegration integration)
		{
			_serivce = integration;
			integration.IsRestoreTransactionsSupported = useFakeRestoreTransactions;
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
}
#endif
