using System;
using System.Threading;
using Content;
using Content.Management;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Content.ScriptableObjects.Editor;
#endif

namespace Booting.Content
{
	[TypeRegistryItem(
		"\u2009Content Management", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.JournalBookmarkFill)]
	[Serializable]
	public class ContentManagementBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 20;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var resolver = new ContentResolver();
			ContentManager.Initialize(resolver);

#if UNITY_EDITOR
			ContentDatabaseEditorUtility.ValidateDatabases();

			if (ClientEditorContentImporterMenu.IsEnable)
				ContentDatabaseEditorMenu.SyncAll();
#endif

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			ContentManager.Terminate();
		}
	}
}
