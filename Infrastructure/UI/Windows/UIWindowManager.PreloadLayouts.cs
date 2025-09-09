using System.Collections.Generic;
using AssetManagement;
using Content;
using Sapientia.Pooling;

namespace UI.Windows
{
	public partial class UIWindowManager
	{
		private AssetsPreloader _preloader = new();

		private void InitializeAssetsPreloader()
		{
			using (ListPool<IAssetReferenceEntry>.Get(out var list))
			{
				foreach (var entry in ContentManager.GetAllEntries<UIWindowEntry>())
				{
					ref readonly var window = ref entry.Value;

					if (!window.layout.HasFlag(LayoutAutomationMode.Preload))
						continue;

					list.Add(window.layout.LayoutReference);
				}

				_preloader.Preload(list.ToArray());
			}
		}

		private void DisposeAssetsPreloader()
		{
			_preloader?.Dispose();
			_preloader = null;
		}

		private void TryReleasePreloadedLayout(IWindow window)
		{
			var entry = ContentManager.Get<UIWindowEntry>(window.Id);

			if (entry.layout.HasFlag(LayoutAutomationMode.AutoDestroy))
				_preloader.TryRelease(entry.layout.LayoutReference);
		}
	}
}
