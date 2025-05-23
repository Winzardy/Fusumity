using UnityEngine;

namespace InputManagement
{
	public enum SwipeDirection
	{
		None,
		Up,
		Down,
		Left,
		Right
	};

	public struct SwipeInfo
	{
		public Vector2 position;
		public Vector2 delta;

		public int touchCount;
		public TouchPhase phase;

		/// <summary>
		/// Время свайпа
		/// </summary>
		public float time;

		public override string ToString()
		{
			return $"Swipe info - " +
				$"Position: [{position}] " +
				$"Delta: [{delta}] " +
				$"Touch Count: [{touchCount}]";
		}
	}

	public static class SwipeInfoExt
	{
		public static SwipeDirection ToDirection(this SwipeInfo touch)
		{
			var normalizedDelta = touch.delta.normalized;
			if (normalizedDelta.y > 0 && normalizedDelta.x > -0.5f && normalizedDelta.x < 0.5f)
				return SwipeDirection.Up;
			else if (normalizedDelta.y < 0 && normalizedDelta.x > -0.5f && normalizedDelta.x < 0.5f)
				return SwipeDirection.Down;
			else if (normalizedDelta.x < 0 && normalizedDelta.y > -0.5f && normalizedDelta.y < 0.5f)
				return SwipeDirection.Left;
			else if (normalizedDelta.x > 0 && normalizedDelta.y > -0.5f && normalizedDelta.y < 0.5f)
				return SwipeDirection.Right;

			return SwipeDirection.None;
		}
	}
}
