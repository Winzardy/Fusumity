using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Utility;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	public class ContentAssetModificationProcessor : AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			var asset = AssetDatabase.LoadAssetAtPath<ContentScriptableObject>(assetPath);
			if (asset)
				ContentAutoConstantsGenerator.ForceInvokeWithDelay(asset.GetType());

			return AssetDeleteResult.DidNotDelete;
		}
	}

	[InitializeOnLoad]
	public static class ContentAutoConstantsGenerator
	{
		private const string MENU_PATH = ContentMenuConstants.CONSTANTS_MENU + "Generate";

		[MenuItem(MENU_PATH)]
		private static void Execute() => ContentDatabaseEditorUtility.TryRunRegenerateConstantsByAuto(true);

		private static CancellationTokenSource _cts;

		static ContentAutoConstantsGenerator() => Invoke();

		public static void Invoke()
		{
			EditorApplication.delayCall += OnDelayCall;

			void OnDelayCall()
			{
				EditorApplication.delayCall -= OnDelayCall;
				ContentDatabaseEditorUtility.TryRunRegenerateConstantsByAuto();
			}

			AsyncUtility.TriggerAndSetNull(ref _cts);
		}

		public static void Invoke(Type type)
		{
			EditorApplication.delayCall += OnDelayCall;
			AsyncUtility.TriggerAndSetNull(ref _cts);

			void OnDelayCall()
			{
				EditorApplication.delayCall -= OnDelayCall;
				ContentDatabaseEditorUtility.TryRegenerateConstants(type);
			}
		}

		public static void ForceInvokeWithDelay(Type type)
		{
			AsyncUtility.TriggerAndSetNull(ref _cts);
			_cts ??= new CancellationTokenSource();
			InvokeDelayAsync(type, _cts.Token).Forget();
		}

		public static async UniTaskVoid InvokeDelayAsync(Type type, CancellationToken cancellationToken)
		{
			await UniTask.Delay(5000, DelayType.Realtime, cancellationToken: cancellationToken);

			ContentAutoConstantsGeneratorMenu.Reset();
			Invoke(type);
			EditorApplication.delayCall += OnDelayCall;

			void OnDelayCall()
			{
				EditorApplication.delayCall -= OnDelayCall;
				ContentAutoConstantsGeneratorMenu.UpdateBlockGenerate();
			}
		}
	}
}
