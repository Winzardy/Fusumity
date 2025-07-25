﻿using System.Threading;
using AssetManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Fusumity.Utility;
using InputManagement;
using Sapientia.ServiceManagement;
using UnityEngine;
using ZenoTween.Utility;

namespace UI
{
	public struct UIObjectInspectorEntry
	{
		public bool useCustomPosition;
		public Vector3 position;

		public bool useCustomRotation;
		public Vector3 rotation;

		public bool useCustomScale;
		public Vector3 scale;

		public Vector2 rotationSensitivity;
		public bool disableRestoreRotation;

		/// <summary>
		/// Задержка перед поворотом на исходную позицию!
		/// </summary>
		public float rotationDelay;

		public Ease rotationEase;
		public float rotationDuration;

		//TODO: надо вынести в настройки...
		public static UIObjectInspectorEntry Default = new()
		{
			useCustomRotation = true,
			rotation = new Vector3(0, 90, 0),
			useCustomPosition = true,
			position = new Vector3(0, -1f, 5f),
			rotationSensitivity = new Vector2(0.25f, 0),
			rotationDelay = 0.25f,
			rotationEase = Ease.InOutCubic,
			rotationDuration = 1.5f
		};
	}

	public interface IObjectInspectorArgs<T>
	{
		public IAssetReferenceEntry reference { get; }
		public T prefab { get; }

		//Для кастомных спинеров
		public ISpinner spinner { get; }

		public UITextureRendererArgs? render { get; }

		public UIObjectInspectorEntry entry { get; }
	}

	public abstract class UIBaseObjectInspector<T, TArgs> : UIWidget<UIObjectInspectorLayout, TArgs>
		where T : Object
		where TArgs : struct, IObjectInspectorArgs<T>
	{
		private const int RELEASE_DELAY_MS = 500;

		private IInputReader _inputReader;

		private UISpinner _defaultSpinner;

		private ISpinner spinner => _args.spinner ?? _defaultSpinner;

		private IAssetReferenceEntry _targetReference;
		private T _targetPrefab;

		protected GameObject _gameObject;
		private Tween _restoreRotationTween;
		private bool _canRotate;

		protected UITextureRenderer _textureRenderer;

		public UIObjectInspectorEntry entry => _args.entry;

		public T target;

		protected override void OnInitialized()
		{
			ServiceLocator.Get(out _inputReader);
			_inputReader.Swiped += OnSwiped;
			_inputReader.Tapped += OnTapped;
		}

		protected override void OnDispose()
		{
			_inputReader.Swiped -= OnSwiped;
			_inputReader.Tapped -= OnTapped;
			_inputReader = null;

			TryClearAll();
			_restoreRotationTween.KillSafe();
		}

		protected override void OnLayoutInstalled()
		{
			if (_layout.spinner)
				CreateWidget(out _defaultSpinner, _layout.spinner);

			_textureRenderer = new UITextureRenderer();
		}

		protected override void OnLayoutCleared()
		{
			_textureRenderer?.Dispose();
			_restoreRotationTween.KillSafe();
		}

		protected override void OnShow(ref TArgs args)
		{
			if (args.render.HasValue)
			{
				_textureRenderer?.Dispose();
				_textureRenderer = new UITextureRenderer(args.render);
			}

			ShowAsync().Forget();
		}

		protected override void OnHide(ref TArgs args)
		{
			TryClearAll();
			_textureRenderer?.Hide();

			_restoreRotationTween.KillSafe();
		}

		private async UniTaskVoid ShowAsync()
		{
			_restoreRotationTween?.KillSafe();

			_layout.image.SetActive(false);

			var prefab = _args.prefab;

			if (prefab == null && !_args.reference.IsEmpty())
			{
				spinner?.SetActive(true);
				prefab = await LoadAsync(_args.reference, DisposeCancellationToken);

				_targetReference?.Release(RELEASE_DELAY_MS);
				_targetReference = _args.reference;

				spinner?.SetActive(false);
			}

			if (prefab != _targetPrefab)
			{
				if (prefab == null)
				{
					GUIDebug.LogError("Prefab is null");
					return;
				}

				TryClearPrefab();
				target = Create(prefab);

				_targetPrefab = prefab;
			}

			if (_targetPrefab == null)
				return;

			var texture = _textureRenderer.Show();
			OnShow(target);

			_layout.image.texture = texture;
			_layout.image.SetActive(true);
		}

		protected abstract T Create(T prefab);

		protected abstract void OnShow(T target);

		private void TryClearAll()
		{
			TryClearPrefab();
			TryClearObjReference();
		}

		private void TryClearObjReference()
		{
			if (_targetReference == null)
				return;

			_targetReference.Release(RELEASE_DELAY_MS);
			_targetReference = null;
		}

		private void TryClearPrefab()
		{
			if (_targetPrefab == null)
				return;

			_targetPrefab = null;

			OnClearPrefab();
			target = null;
		}

		protected virtual void OnClearPrefab()
		{
		}

		private void OnTapped(TapInfo info)
		{
			if (!IsRotateEnabled())
				return;

			if (entry.disableRestoreRotation)
				return;

			if (info.touchPhase == TouchPhase.Began)
			{
				if (UnityEngine.RectTransformUtility.RectangleContainsScreenPoint(_layout.interactionRect, info.position))
				{
					_restoreRotationTween.KillSafe();
					_restoreRotationTween = null;

					_canRotate = true;
				}
			}

			if (info.touchPhase != TouchPhase.Ended)
				return;

			_canRotate = false;

			_restoreRotationTween ??= _textureRenderer.FocusPoint.transform
			   .DOLocalRotate(Vector3.zero, entry.rotationDuration)
			   .SetEase(entry.rotationEase)
			   .SetDelay(entry.rotationDelay);
		}

		private void OnSwiped(SwipeInfo info)
		{
			if (info.phase == TouchPhase.Ended)
				return;

			if (!IsRotateEnabled())
				return;

			if (!_canRotate)
				return;

			var x = info.delta.y * entry.rotationSensitivity.y;
			var y = -info.delta.x * entry.rotationSensitivity.x;
			_textureRenderer.FocusPoint.transform.Rotate(x, y, 0f);
		}

		private bool IsRotateEnabled()
		{
			if (_layout == null || target == null)
				return false;

			if (entry.rotationSensitivity == Vector2.zero)
				return false;

			return true;
		}

		protected override void OnReset(bool _)
		{
			base.OnReset(_);
			_restoreRotationTween?.KillSafe();
		}

		protected abstract UniTask<T> LoadAsync(IAssetReferenceEntry reference,
			CancellationToken cancellationToken);
	}
}
