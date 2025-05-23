using System;
using Content;
using Fusumity;
using Fusumity.Utility;
using Fusumity.Utility.Camera;
using UnityEngine;

namespace UI
{
	using UnityObject = UnityEngine.Object;
	using CameraRenderType = Content.Constants.Fusumity.CameraRenderType;

	public struct RenderTextureArgs
	{
		public int width;
		public int height;
		public int depth;
		public RenderTextureFormat format;
	}

	public struct UITextureRendererArgs
	{
		private const string DEFAULT_LAYER_NAME = "UI_Avatar";
		public static int DEFAULT_LAYER => LayerMask.NameToLayer(DEFAULT_LAYER_NAME);

		public RenderTextureArgs renderTexture;
		public CameraRenderEntry cameraRender;

		public int layer;

		public static UITextureRendererArgs GetDefault(int layer)
		{
			var cameraEntry = ContentManager.Get<CameraRenderEntry>(CameraRenderType.UI_OBJECT_INSPECTION);
			cameraEntry.cullingMask |= 1 << layer;

			return new()
			{
				renderTexture =
					new RenderTextureArgs {width = 768, height = 768, depth = 24, format = RenderTextureFormat.ARGB32,},
				cameraRender = cameraEntry,
				layer = layer
			};
		}
	}

	public class UITextureRenderer : IDisposable
	{
		private const string FOCUS_NAME = "Focus Point";

		private UITextureRendererArgs _args;

		private Camera _camera;
		private RenderTexture _texture;

		private GameObject _focusPoint;

		public Transform FocusPoint
		{
			get
			{
				if (_focusPoint == null)
					_focusPoint = CreateFocusPoint();

				return _focusPoint.transform;
			}
		}

		public Camera Camera
		{
			get
			{
				if (_camera == null)
					_camera = GetCameraAndSetup();

				return _camera;
			}
		}

		public UITextureRenderer(UITextureRendererArgs? args = null)
		{
			args ??= UITextureRendererArgs.GetDefault(UITextureRendererArgs.DEFAULT_LAYER);
			_args = args.Value;
		}

		public void Dispose()
		{
			Release();

			if (!_camera)
				return;

			UICameraPool.Release(_camera);
			UnityObject.Destroy(_focusPoint);
		}

		public void FocusTo(GameObject target)
		{
			target.transform.SetParent(_focusPoint.transform);
			target.transform.localPosition = Vector3.zero;

			target.transform.SetLayerRecursive(_args.layer);
		}

		public RenderTexture Show()
		{
			if (!_texture)
			{
				_texture = new RenderTexture
				(
					_args.renderTexture.width,
					_args.renderTexture.height,
					_args.renderTexture.depth,
					_args.renderTexture.format
				)
				{
					antiAliasing = 2,
					name = "[UI] Rendered Texture"
				};

				_texture.Create();

				Camera.targetTexture = _texture;
			}

			Camera.enabled = true;

			return _texture;
		}

		public void Hide()
		{
			if (_camera)
				_camera.enabled = false;
		}

		public void Release()
		{
			if (!_texture)
				return;

			_texture.Release();
			_texture = null;

			if (_camera)
				_camera.targetTexture = null;
		}

		private Camera GetCameraAndSetup()
		{
			var camera = UICameraPool.Get();
			camera.Setup(_args.cameraRender);

			return camera;
		}

		private GameObject CreateFocusPoint()
		{
			var gameObject = new GameObject(FOCUS_NAME);
			var transform = gameObject.transform;
			transform.SetParent(Camera.transform);
			transform.localPosition = Vector3.forward * 5;
			transform.localRotation = Quaternion.identity;

			return gameObject;
		}
	}
}
