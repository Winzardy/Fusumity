using AssetManagement;
using Content;
using Sapientia.Pooling;

namespace UI.Screens
{
	public partial class UIScreenManager
	{
		private AssetsPreloader _preloader = new();

		private void InitializeAssetsPreloader()
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var list))
			{
				foreach (var entry in ContentManager.GetAllEntries<UIScreenEntry>())
				{
					ref readonly var screen = ref entry.Value;

					if (!screen.layout.HasFlag(LayoutAutomationMode.Preload))
						continue;

					list.Add(screen.layout.LayoutReference);
				}

				_preloader.Preload(list.ToArray());
			}
		}

		private void DisposeAssetsPreloader()
		{
			_preloader?.Dispose();
			_preloader = null;
		}

		private void TryReleasePreloadedLayout(IScreen screen)
		{
			var entry = ContentManager.Get<UIScreenEntry>(screen.Id);

			if (entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy))
				_preloader.TryRelease(entry.layout.LayoutReference);
		}
	}
}
