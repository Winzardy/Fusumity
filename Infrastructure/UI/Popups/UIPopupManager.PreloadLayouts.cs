using AssetManagement;
using Content;
using Sapientia.Pooling;

namespace UI.Popups
{
	public partial class UIPopupManager
	{
		private AssetsPreloader _preloader = new();

		private void InitializeAssetsPreloader()
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var list))
			{
				foreach (var entry in ContentManager.GetAllEntries<UIPopupConfig>())
				{
					ref readonly var popup = ref entry.Value;

					if (!popup.layout.HasFlag(LayoutAutomationMode.Preload))
						continue;

					list.Add(popup.layout.LayoutReference);
				}

				_preloader.Preload(list.ToArray());
			}
		}

		private void DisposeAssetsPreloader()
		{
			_preloader.Dispose();
			_preloader = null;
		}

		private void TryReleasePreloadedLayout(IPopup popup)
		{
			var entry = ContentManager.Get<UIPopupConfig>(popup.Id);
			if (entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy))
				_preloader.TryRelease(entry.layout.LayoutReference);
		}
	}
}
