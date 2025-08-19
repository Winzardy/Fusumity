using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AssetManagement;
using Sapientia.Utility;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Виджет с автоматической подгрузкой верстки, если она active
	/// При SetActive(true) подгружает верстку
	/// </summary>
	public abstract class UISelfConstructedWidget<TLayout> : UIWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		private const string LAYOUT_PREFIX_NAME = "[UI] ";

		private bool _setupTemplate;

		private CancellationTokenSource _loadTemplateCts;
		private CancellationTokenSource _autoDestroyCts;

		protected abstract RectTransform LayerRectTransform { get; }

		protected abstract ComponentReferenceEntry LayoutReference { get; }
		protected virtual bool LayoutAutoDestroy => false;
		protected virtual int LayoutAutoDestroyDelayMs => 5000;
		protected virtual List<AssetReferenceEntry> PreloadAssets => null;

		protected virtual string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		private protected override void OnDisposeInternal()
		{
			TryCancelCreateLayout();
			TryLayoutClearingAndReleaseTemplate();

			base.OnDisposeInternal();
		}

		protected sealed override void OnActivatedInternal(bool immediate)
		{
			TryStopAutoDestroy();

			if (_layout)
			{
				base.OnActivatedInternal(immediate);
				return;
			}

			WaitSetupTemplateAndActivateAsync(immediate).Forget();
		}

		protected sealed override void OnDeactivatedInternal(bool immediate)
		{
			TryCancelCreateLayout();

			base.OnDeactivatedInternal(immediate);
		}

		protected override bool AutomaticLayoutClearingInternal()
		{
			if (LayoutAutoDestroy)
			{
				if (LayoutAutoDestroyDelayMs <= 0)
				{
					TryLayoutClearingAndReleaseTemplate();
					return true;
				}

				WaitBeforeDestroyAsync().Forget();

				return false;
			}

			return false;
		}

		protected override void LayoutClearingInternal()
		{
			if (!_layout)
				return;

			OnLayoutClearedInternal();
			DisposeAndClearChildren();

			if (_setupTemplate)
				UIFactory.Destroy(_layout);

			_layout = null;
		}

		protected override bool ValidateLayout(out string msg)
		{
			msg = null;
			return true;
		}

		private async UniTaskVoid WaitSetupTemplateAndActivateAsync(bool immediate)
		{
			if (_loadTemplateCts != null)
				return;

			_loadTemplateCts = new CancellationTokenSource();

			try
			{
				await SetupTemplateAndActivateAsync(immediate, _loadTemplateCts.Token);
			}
			catch (OperationCanceledException)
			{
				GUIDebug.LogWarning($"{GetType()} layout (template) loading was canceled");
			}
			catch (Exception e)
			{
				GUIDebug.LogException(e);
			}
			finally
			{
				AsyncUtility.Release(ref _loadTemplateCts);
			}
		}

		private void TryStopAutoDestroy()
			=> AsyncUtility.Trigger(ref _autoDestroyCts);

		private async UniTaskVoid WaitBeforeDestroyAsync()
		{
			if (_autoDestroyCts != null)
				return;

			_autoDestroyCts = new CancellationTokenSource();

			var originalName = _layout.name;
			try
			{
				var cancellationToken = _autoDestroyCts.Token;

				await DelayBeforeDestroyAsync(cancellationToken, originalName);

				//Подождать анимацию если она есть
				if (Visible)
					await UniTask.WaitWhile(() => Visible, cancellationToken: _autoDestroyCts.Token);

				TryLayoutClearingAndReleaseTemplate();
			}
			finally
			{
				AsyncUtility.Release(ref _autoDestroyCts);

				if (_layout)
					_layout.name = originalName;
			}
		}

		private async UniTask SetupTemplateAndActivateAsync(bool immediate, CancellationToken cancellationToken)
		{
			var template = await LayoutReference.LoadAsync<TLayout>(cancellationToken);
			TryClearTemplate();
			await SetupTemplateAsync(template, cancellationToken);
#if UNITY_EDITOR
			_layout.prefab = LayoutReference.EditorAsset;
#endif
			base.OnActivatedInternal(immediate);
		}

		private async UniTask SetupTemplateAsync(TLayout template, CancellationToken cancellationToken)
		{
			TryLayoutClearingAndReleaseTemplate();

			var layout = await UIFactory.CreateLayoutAsync(template, LayerRectTransform, LayoutPrefixName, cancellationToken);
			SetupLayout(layout);
			SetVisibleInternal(false, false);

			PreloadAssets?.Preload();
			_setupTemplate = true;
		}

		private void TryLayoutClearingAndReleaseTemplate()
		{
			TryStopAutoDestroy();
			LayoutClearingInternal();

			TryClearTemplate();
			SetVisibleInternal(false);
		}

		private void TryClearTemplate()
		{
			if (!_setupTemplate)
				return;

			LayoutReference.Release();
			PreloadAssets?.Release();

			_setupTemplate = false;
		}

		private void TryCancelCreateLayout()
			=> AsyncUtility.Trigger(ref _loadTemplateCts);

		#region Prepare

		protected IDisposable Prepare(Action callback)
		{
			PreloadAsync(callback).Forget();
			return new PrepareDisposer(Release);
			void Release() => LayoutReference.Release();
		}

		private async UniTaskVoid PreloadAsync(Action callback)
		{
			await LayoutReference.PreloadAsync();
			callback?.Invoke();
		}

		private class PrepareDisposer : IDisposable
		{
			private Action _onDispose;

			public PrepareDisposer(Action onDispose) => _onDispose = onDispose;

			public void Dispose() => _onDispose?.Invoke();
		}

		#endregion

		private async UniTask DelayBeforeDestroyAsync(CancellationToken cancellationToken, string originalName)
		{
#if !UNITY_EDITOR
			await UniTask.Delay(LayoutAutoDestroyDelayMs, true, cancellationToken: cancellationToken);
#else
			var leftMs = LayoutAutoDestroyDelayMs;

			await UniTask.NextFrame(cancellationToken: cancellationToken);

			leftMs -= (int) (Time.unscaledDeltaTime * 1000);
			if (_layout)
				originalName = _layout.name;

			while (leftMs > 0)
			{
				await UniTask.NextFrame(cancellationToken: cancellationToken);

				leftMs -= (int) (Time.unscaledDeltaTime * 1000);
				var seconds = leftMs / 1000f;

				if (_layout)
					_layout.name = originalName + $" (left: {seconds:F2} sec)";
			}
#endif
		}
	}
}
