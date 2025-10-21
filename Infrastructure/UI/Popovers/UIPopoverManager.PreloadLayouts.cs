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
				foreach (var entry in ContentManager.GetAllEntries<UIPopoverConfig>())
				{
					ref readonly var popover = ref entry.Value;

					if (!popover.layout.HasFlag(LayoutAutomationMode.Preload))
						continue;

					list.Add(popover.layout.LayoutReference);
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
			var entry = ContentManager.Get<UIPopoverConfig>(popup.Id);
			if (entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy))
				_preloader.TryRelease(entry.layout.LayoutReference);
		}
	}
}
