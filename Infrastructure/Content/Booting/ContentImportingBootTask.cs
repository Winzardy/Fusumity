using System;
using System.Threading;
using AssetManagement;
using Content;
using Content.ScriptableObjects;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Booting.Content
{
	[TypeRegistryItem(
		"\u2009Content Import", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.JournalArrowUp)]
	[Serializable]
	public class ContentImportingBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 110;

		public AssetLabelReferenceEntry label;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			var importer = new ClientContentImporter(label);
			await ContentManager.PopulateAsync(importer, token);
		}
	}
}
