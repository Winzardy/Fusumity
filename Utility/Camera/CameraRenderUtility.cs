using UnityEngine.Rendering.Universal;

namespace Fusumity.Utility.Camera
{
	using UnityCamera = UnityEngine.Camera;

	public static class CameraRenderUtility
	{
		public static void Setup(this UnityCamera camera, CameraRenderEntry entry)
		{
			camera.cullingMask = entry.cullingMask;

			camera.nearClipPlane = entry.nearClipPlane;
			camera.farClipPlane = entry.farClipPlane;

			camera.clearFlags = entry.clearFlags;
			camera.backgroundColor = entry.backgroundColor;

			camera.fieldOfView = entry.fov;

			var urpData = camera.GetUniversalAdditionalCameraData();

			if (entry.useRenderIndex)
				urpData.SetRenderer(entry.renderIndex);

			camera.orthographic = entry.orthographic;
			camera.orthographicSize = entry.orthographicSize;

			urpData.volumeLayerMask = entry.volumeLayerMask;
		}
	}
}
