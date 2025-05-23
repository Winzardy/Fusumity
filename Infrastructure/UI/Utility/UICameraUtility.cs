using Fusumity.Utility;
using JetBrains.Annotations;
using UnityEngine;

namespace UI
{
	public enum ClippingType
	{
		Ellipse = 0,
		Circle = 1,
		Rectangle = 2
	}

	public struct CalculateScreenTransformInput
	{
		public Vector3 worldPosition;
		public Vector3? size;
		public float radius;

		public bool offscreen;

		/// <summary>
		/// Скрывать offscreen маркер если он во фрустум камеры
		/// </summary>
		public bool hideOffscreenInFrustum;

		public ClippingType? clippingType;

		[CanBeNull]
		public RectTransform area;

		public bool disableIsTargetOnFrustumCheck;
	}

	public struct CalculateScreenTransformOutput
	{
		public Vector3 position;
		public Vector2 direction;

		public bool offscreen;

		//Объект во фруструме камеры
		public bool targetOnCameraFrustum;
	}

	public static class UICameraUtility
	{
		public static readonly Vector3 DEFAULT_SIZE = new(2, 1, 1);

		public static void CalculateScreenTransform(this Camera camera,
			in CalculateScreenTransformInput input,
			out CalculateScreenTransformOutput output)
		{
			output = new CalculateScreenTransformOutput();

			var radius = input.radius;
			var offset = Vector3.down * radius;

			var position = input.worldPosition;

			output.targetOnCameraFrustum = input.disableIsTargetOnFrustumCheck || camera.IsTargetOnFrustum(input, offset);

			if (output.targetOnCameraFrustum)
			{
				output.direction = Vector2.down;
				output.position = camera.WorldToScreenPoint(position);
				output.offscreen = false;
				return;
			}

			if (!input.offscreen)
				return;

			output.offscreen = true;
			var fromCameraToTarget = position - camera.transform.position;

			var cameraForward = camera.transform.forward.normalized;
			var distance = Vector3.Dot(cameraForward, fromCameraToTarget);

			// поместить цель на ближнюю плоскость отсечения, это не изменит проекцию цели, но позволит избежать артефактов
			// когда цель находится за ближней плоскостью отсечения.
			if (distance < camera.nearClipPlane)
			{
				position += cameraForward * (camera.nearClipPlane - distance);

				// TODO: Если цель находится прямо позади, то ее проекция будет внутри области просмотра,
				// поэтому заставляем ее придерживаться границ экрана. В нашей игре это сейчас невозможно.
			}

			var screenPosition = camera.WorldToScreenPoint(position);
			screenPosition += offset;

			Vector2 screenPoint = screenPosition;

			var rect = input.area ? input.area.ToScreenSpace() : new Rect(0, 0, Screen.width, Screen.height);

			var clippingType = input.clippingType ?? ClippingType.Ellipse;

			switch (clippingType)
			{
				case ClippingType.Ellipse:
					ClippingUtility.ClipLineToEllipse(rect.center, in screenPoint, rect.center,
						rect.GetMinRectSide() / 2,
						rect.GetMaxRectSide() / 2,
						out screenPoint, out _);
					break;
				case ClippingType.Circle:
					ClippingUtility.ClipLineToCircle(in screenPoint, rect.center, rect.GetMinRectSide() / 2,
						out screenPoint);
					break;
				case ClippingType.Rectangle:
					ClippingUtility.ClipLineToRect(rect.center, in screenPoint, rect,
						out _, out screenPoint);
					break;
			}

			output.direction = (screenPoint - rect.center).normalized;

			screenPoint -= radius * output.direction;

			output.position = screenPoint;
		}

		public static bool IsTargetOnFrustum(this Camera camera, Vector3 worldPosition)
			=> IsTargetOnFrustum(camera, worldPosition, Vector3.zero);

		public static bool IsTargetOnFrustum(this Camera camera, Vector3 worldPosition, Vector3 offset)
		{
			if (!camera)
				return false;

			var screenPoint = camera.WorldToScreenPoint(worldPosition) + offset;

			if (screenPoint.z <= 0)
				return false;

			return screenPoint.x >= 0 &&
				screenPoint.x <= Screen.width &&
				screenPoint.y >= 0 &&
				screenPoint.y <= Screen.height;
		}

		public static bool IsTargetOnFrustum(this Camera camera, in CalculateScreenTransformInput input, Vector3? offset = null)
		{
			if (offset.HasValue)
				return camera.IsTargetOnFrustum(input.worldPosition, input.size ?? DEFAULT_SIZE, offset.Value);

			var radius = input.radius;
			offset = Vector3.down * radius;

			return camera.IsTargetOnFrustum(input.worldPosition, input.size ?? DEFAULT_SIZE, offset.Value);
		}

		public static bool IsTargetOnFrustum(this Camera camera, Vector3 worldPosition, Vector3 size, Vector3 offset)
		{
			if (size == default)
				return camera.IsTargetOnFrustum(worldPosition, offset);

			var halfSize = size * 0.5f;

			return camera.IsSideOnFrustum(worldPosition, Vector3.up, halfSize.z, offset) ||
				camera.IsSideOnFrustum(worldPosition, Vector3.down, halfSize.z, offset) ||
				camera.IsSideOnFrustum(worldPosition, Vector3.left, halfSize.x, offset) ||
				camera.IsSideOnFrustum(worldPosition, Vector3.right, halfSize.x, offset) ||
				camera.IsSideOnFrustum(worldPosition, Vector3.forward, halfSize.y, offset) ||
				camera.IsSideOnFrustum(worldPosition, Vector3.back, halfSize.y, offset);
		}

		private static bool IsSideOnFrustum(this Camera camera, Vector3 target, Vector3 direction, float distance,
			Vector3 screenPointOffset)
		{
			target += direction * distance;

			return camera.IsTargetOnFrustum(target, screenPointOffset);
		}
	}
}
