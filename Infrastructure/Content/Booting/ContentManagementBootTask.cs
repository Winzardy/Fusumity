using System;
using System.Threading;
using Content;
using Content.Management;
using Content.ScriptableObjects;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sirenix.OdinInspector;

namespace Booting.Content
{
#if UNITY_EDITOR
	using ContentImporter = EditorClientContentImporter;
#else
	using ContentImporter = BuildInClientContentImporter;
#endif

	[TypeRegistryItem(
		"\u2009Content Management", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.JournalBookmarkFill)]
	[Serializable]
	public class ContentManagementBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 20;

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			var importer = new ContentImporter();
			var management = new ContentResolver(importer);
			await management.InitializeAsync(token);
			ContentManager.Initialize(management);
		}

		protected override void OnDispose()
		{
			ContentManager.Terminate();
		}
	}
}
