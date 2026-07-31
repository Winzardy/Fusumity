using System;
using System.Threading;
using AssetManagement;
using Content;
using Content.ScriptableObjects;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Booting.Content
{
	[TypeRegistryItem(
		"\u2009Content Import", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.JournalArrowUp)]
	[Serializable]
	public class ContentImportBootTask : BaseBootTask, IWeightedProgress
	{
		public override int Priority => HIGH_PRIORITY - 110;

		public AssetLabelReference label;
		public bool waitForPreviousTasks;

		[SerializeField]
		private float _weight;

		public float Weight => _weight;

		public override string Name { get => $"{base.Name} ({label})"; }
		public override bool WaitForPreviousTasks { get => waitForPreviousTasks; }

		protected override async UniTask RunTaskAsync(Blackboard _, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			var importer = new ClientContentImporter(label);
			var importProgress = progress != null ? new Progress<float>(f => progress?.Report(new BootProgressInfo(_loadingLocKey, f))) : null;
			await ContentManager.PopulateAsync(importer, new WaitFramesAsyncFlowController(100, 1), importProgress, token);
		}
	}
}
