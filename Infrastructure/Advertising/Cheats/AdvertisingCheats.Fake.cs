#if DebugLog
using System;
using Advertising.Fake;
using Cysharp.Threading.Tasks;
using MobileConsole;

namespace Advertising.Cheats.Fake
{
#if !UNITY_EDITOR
	[SettingCommand(name = "System/" + nameof(Advertising))]
	public class AdvertisingFakeCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeUpdated))]
		public bool useFake;

		private IAdvertisingIntegration _main;
		private IAdvertisingIntegration _fake;

		public void OnUseFakeUpdated()
		{
			if (!AdManager.IsInitialized)
				return;

			if (useFake)
			{
				var skip = false;
				if (AdManager.Integration is FakeAdIntegration integration)
				{
					TrySetFake(integration);
					skip = true;
				}
				else if (_fake == null)
					TrySetFake(new FakeAdIntegration());

				if (skip)
					return;

				var prev = AdManager.SetIntegration(_fake);
				_main ??= prev;
			}
			else if (_main != null)
			{
				AdManager.SetIntegration(_main);
			}
		}

		private void TrySetFake(FakeAdIntegration integration)
		{
			if (_fake != null)
				return;

			_fake = integration;
			LogConsole.GetCommand<AdvertisingFakeOverlayCheats>().SetFake((FakeAdIntegration) _fake);
		}
	}
#endif

	[SettingCommand(name = "System/" + nameof(Advertising))]
	public class AdvertisingFakeOverlayCheats : Command
	{
		[Variable(OnValueChanged = nameof(OnUseFakeOverlayUpdated))]
		public bool useFakeOverlay;

		private FakeAdIntegration _integration;

		public void OnUseFakeOverlayUpdated()
		{
			if (TryGetIntegration(out var integration))
				integration.SetOverlay(useFakeOverlay);
		}

		public override void OnVariableValueLoaded()
		{
			SetVariablesAsync().Forget();
		}

		public override void InitDefaultVariableValue()
		{
			if (TryGetIntegration(out _integration))
				useFakeOverlay = _integration.UseOverlay;
			else
				useFakeOverlay = FakeAdIntegration.DEFAULT_USE_OVERLAY;
		}

		private bool TryGetIntegration(out FakeAdIntegration integration)
		{
			integration = _integration;

			if (_integration != null)
				return true;

			if (!AdManager.IsInitialized)
				return false;

			if (AdManager.Integration is not FakeAdIntegration x)
				return false;

			SetFake(x);
			integration = x;
			return true;
		}

		public void SetFake(FakeAdIntegration integration)
		{
			_integration = integration;
			integration.SetOverlay(useFakeOverlay);
			refreshUI?.Invoke();
		}

		private async UniTaskVoid SetVariablesAsync()
		{
			await UniTask.WaitUntil(() => AdManager.IsInitialized)
			   .Timeout(TimeSpan.FromSeconds(30), DelayType.Realtime);

			if (TryGetIntegration(out var integration))
				integration.SetOverlay(useFakeOverlay);
		}
	}
}
#endif
