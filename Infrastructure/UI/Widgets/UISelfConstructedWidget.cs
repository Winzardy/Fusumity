using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AssetManagement;
using Sapientia;
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

		protected TLayout _template;

		private CancellationTokenSource _setupTemplateCts;
		private CancellationTokenSource _autoDestroyCts;

		protected abstract RectTransform LayerRectTransform { get; }

		protected abstract ComponentReferenceEntry LayoutReference { get; }
		protected virtual bool LayoutAutoDestroy => false;
		protected virtual int LayoutAutoDestroyDelayMs => 5000;
		protected virtual List<AssetReferenceEntry> PreloadAssets => null;

		protected virtual string LayoutPrefixName => LAYOUT_PREFIX_NAME;

		private protected override void OnDisposeInternal()
		{
			CancelSetupLayout();
			LayoutClearingAndReleaseTemplateSafe();

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
			if (!suppressFlag.HasFlag(SuppressFlag.Events))
				CancelSetupLayout();

			base.OnDeactivatedInternal(immediate);
		}

		protected override bool AutomaticLayoutClearingInternal()
		{
			if (LayoutAutoDestroy)
			{
				if (LayoutAutoDestroyDelayMs <= 0)
				{
					LayoutClearingAndReleaseTemplateSafe();
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

			if (_template)
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
			if (_setupTemplateCts != null)
				return;

			_setupTemplateCts = new CancellationTokenSource();
			_setupTemplateCts.Token
			   .Register(OnTriggered);

			try
			{
				await SetupTemplateAndActivateAsync(immediate, _setupTemplateCts.Token);
			}
			catch (OperationCanceledException)
			{
				GUIDebug.LogWarning($"[ {GetType()} ] template loading was canceled");
			}
			catch (Exception e)
			{
				GUIDebug.LogException(e);
			}
			finally
			{
				AsyncUtility.Release(ref _setupTemplateCts);
			}

			void OnTriggered() => GUIDebug.LogWarning($"[ {GetType()} ] template loading cancel was triggered");
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

				LayoutClearingAndReleaseTemplateSafe();
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
			ClearTemplateSafe();
			await SetupTemplateAsync(template, cancellationToken);
#if UNITY_EDITOR
			_layout.prefab = LayoutReference.EditorAsset;
#endif
			base.OnActivatedInternal(immediate);
		}

		private async UniTask SetupTemplateAsync(TLayout template, CancellationToken cancellationToken)
		{
			LayoutClearingAndReleaseTemplateSafe();

			var layout = await UIFactory.CreateLayoutAsync(template, LayerRectTransform, LayoutPrefixName, cancellationToken);
			SetupLayout(layout);

			EnableSuppress(SuppressFlag.Events);
			{
				SetVisibleInternal(false, false);
				SetActiveInternal(false, true);
			}
			DisableSuppress();

			// ReSharper disable once MethodHasAsyncOverload
			PreloadAssets?.Preload(cancellationToken);
			_template = template;
		}

		private void LayoutClearingAndReleaseTemplateSafe()
		{
			TryStopAutoDestroy();
			LayoutClearingInternal();

			ClearTemplateSafe();

			EnableSuppress(SuppressFlag.Events);
			{
				SetVisibleInternal(false);
			}
			DisableSuppress();
		}

		private void ClearTemplateSafe()
		{
			if (!_template)
				return;

			LayoutReference.Release();
			PreloadAssets?.Release();

			_template = null;
		}

		private void CancelSetupLayout()
			=> AsyncUtility.Trigger(ref _setupTemplateCts);

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
			int delayMs =
#if !DebugLog
				LayoutAutoDestroyDelayMs;
#else
				UILayoutDebug.debugDelayMs
					? UILayoutDebug.debugDelayMs
					: LayoutAutoDestroyDelayMs;
#endif

#if !UNITY_EDITOR
			await UniTask.Delay(delay, true, cancellationToken: cancellationToken);
#else
			await UniTask.NextFrame(cancellationToken: cancellationToken);

			delayMs -= (int) (Time.unscaledDeltaTime * 1000);
			if (_layout)
				originalName = _layout.name;

			while (delayMs > 0)
			{
				await UniTask.NextFrame(cancellationToken: cancellationToken);

				delayMs -= (int) (Time.unscaledDeltaTime * 1000);
				var seconds = delayMs / 1000f;

				if (_layout)
					_layout.name = originalName + $" (left: {seconds:F2} sec)";
			}
#endif
		}

#if DebugLog
		public static class UILayoutDebug
		{
			public static Toggle<int> debugDelayMs = 3000;
		}
#endif
	}
}
