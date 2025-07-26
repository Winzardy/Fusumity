using UnityEngine;

namespace Game.Logic.Gizmo
{
	public class GizmoDrawer : MonoBehaviour
	{
		private const int _framesToDetectDisabling = 1;

		private int _lastDrawnFrame = Time.frameCount;

		private void Update()
		{
			GizmoExt.IsEnabled = (Time.frameCount -_lastDrawnFrame) <= _framesToDetectDisabling;
		}

		private void OnDrawGizmos()
		{
			GizmoExt.DrawGizmo();
			_lastDrawnFrame = Time.frameCount;
		}
	}
}
