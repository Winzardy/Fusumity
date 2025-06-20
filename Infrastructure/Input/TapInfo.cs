using UnityEngine;

namespace InputManagement
{
	public struct TapInfo
	{
		public TouchPhase touchPhase;
		public Vector2 position;
		public int fingerId;

		public TapInfo(TouchPhase touchPhase, Vector2 position, int fingerId = -1)
		{
			this.touchPhase = touchPhase;
			this.position = position;
			this.fingerId = fingerId;
		}

		public override string ToString()
		{
			return $"Tap info - " +
				$"Phase: [{touchPhase}] " +
				$"Position: [{position}] " +
				$"Finger Id: [{fingerId}]";
		}
	}
}