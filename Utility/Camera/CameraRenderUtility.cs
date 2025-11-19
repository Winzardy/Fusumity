using UnityEngine.Rendering.Universal;

namespace Fusumity.Utility.Camera
{
	using UnityCamera = UnityEngine.Camera;

	public static class CameraRenderUtility
	{
		public static void Setup(this UnityCamera camera, CameraRenderSettings settings)
		{
			camera.cullingMask = settings.cullingMask;

			camera.nearClipPlane = settings.nearClipPlane;
			camera.farClipPlane = settings.farClipPlane;

			camera.clearFlags = settings.clearFlags;
			camera.backgroundColor = settings.backgroundColor;

			camera.fieldOfView = settings.fov;

			var urpData = camera.GetUniversalAdditionalCameraData();

			if (settings.useRenderIndex)
				urpData.SetRenderer(settings.renderIndex);

			camera.orthographic = settings.orthographic;
			camera.orthographicSize = settings.orthographicSize;

			urpData.volumeLayerMask = settings.volumeLayerMask;
		}
	}
}
