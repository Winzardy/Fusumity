using UnityEngine;
using UnityEngine.EventSystems;

namespace Audio
{
	/// <summary>
	/// AudioUtility
	/// </summary>
	public static class AudioUtility
	{
		public static Vector3 GetAudioSpatialPosition(this PointerEventData eventData, float? minDistance = null) =>
			GetAudioSpatialPosition(eventData.position, minDistance);

		public static Vector3 GetAudioSpatialPosition(this RectTransform transform, float? minDistance = null)
		{
			var anchorMin = transform.anchorMin;
			var position = new Vector2(anchorMin.x * Screen.width, anchorMin.y * Screen.height);
			return GetAudioSpatialPosition(position, minDistance);
		}

		public static Vector3 GetAudioSpatialPosition(this Vector2 screenPoint, float? minDistance = null)
		{
			float screenX = Screen.width;
			float screenY = Screen.height;

			var x = (screenPoint.x / screenX) - 0.5f;
			var y = (screenPoint.y / screenY) - 0.5f;

			x *= minDistance ?? AudioSpatialScheme.DEFAULT_AUDIO_SPATIAL_DISTANCE_MIN;
			y *= minDistance ?? AudioSpatialScheme.DEFAULT_AUDIO_SPATIAL_DISTANCE_MIN;

			var vector3 = new Vector3(x, y, 0);

			var listenerTransform = AudioManager.GetListener().transform;
			var listenerPosition = listenerTransform.position;
			var listenerRotation = listenerTransform.rotation;

			return listenerPosition + listenerRotation * vector3;
		}
	}
}
