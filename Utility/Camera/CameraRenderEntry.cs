using System;
using Content;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity
{
	[Serializable]
	[Constants]
	public struct CameraRenderEntry
	{
		public LayerMask cullingMask;

		public float fov;
		public float nearClipPlane;
		public float farClipPlane;

		public CameraClearFlags clearFlags;

		[ShowIf(nameof(clearFlags), CameraClearFlags.Color)]
		public Color backgroundColor;

		public bool orthographic;

		[ShowIf(nameof(orthographic))]
		public float orthographicSize;

		[Header("URP:")]
		public bool useRenderIndex;

		public int renderIndex;

		public LayerMask volumeLayerMask;
	}
}
