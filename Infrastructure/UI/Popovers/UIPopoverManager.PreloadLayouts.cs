using AssetManagement;
using Content;
using Sapientia.Pooling;

namespace UI.Popovers
{
	public partial class UIPopoverManager
	{
		private AssetsPreloader _preloader = new();

		private void InitializeAssetsPreloader()
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var list))
			{
				foreach (var entry in ContentManager.GetAll<UIPopoverEntry>())
				{
					ref readonly var tooltip = ref entry.Value;

					if (!tooltip.layout.HasFlag(LayoutAutomationMode.Preload))
						continue;

					list.Add(tooltip.layout.LayoutReference);
				}

				_preloader.Preload(list.ToArray());
			}
		}

		private void DisposeAssetsPreloader()
		{
			_preloader.Dispose();
			_preloader = null;
		}

		private void TryReleasePreloadedLayout(IPopover popup)
		{
			var entry = ContentManager.Get<UIPopoverEntry>(popup.Id);
			if (entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy))
				_preloader.TryRelease(entry.layout.LayoutReference);
		}
	}
}
