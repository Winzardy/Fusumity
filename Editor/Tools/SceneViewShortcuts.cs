using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Fusumity.Editor
{
	public static class SceneViewShortcuts
	{
		[Shortcut("Scene View Camera - Top view", KeyCode.UpArrow, ShortcutModifiers.Control)]
		public static void TopView()
		{
			MakeSceneViewCameraLookAtPivot(Quaternion.Euler(90, 0, 0));
		}

		[Shortcut("Scene View Camera - Right view", KeyCode.RightArrow, ShortcutModifiers.Control)]
		public static void RightView()
		{
			MakeSceneViewCameraLookAtPivot(Quaternion.Euler(0, -90, 0));
		}

		[Shortcut("Scene View Camera - Left view", KeyCode.LeftArrow, ShortcutModifiers.Control)]
		public static void LeftView()
		{
			MakeSceneViewCameraLookAtPivot(Quaternion.Euler(0, 90, 0));
		}

		[Shortcut("Scene View Camera - Front view", KeyCode.DownArrow, ShortcutModifiers.Control)]
		public static void FrontView()
		{
			MakeSceneViewCameraLookAtPivot(Quaternion.Euler(0, 0, 0));
		}

		[Shortcut("Scene View Camera - Opposite view", KeyCode.Period, ShortcutModifiers.Control)]
		public static void ToggleView()
		{
			var camera = SceneView.lastActiveSceneView.camera;
			var currentRot = camera.transform.rotation.eulerAngles;

			if (currentRot == Vector3.zero)
			{
				MakeSceneViewCameraLookAtPivot(Quaternion.Euler(0, 180, 0));
			}
			else if (currentRot == new Vector3(0, 180, 0))
			{
				MakeSceneViewCameraLookAtPivot(Quaternion.Euler(Vector3.zero));
			}
			else
			{
				MakeSceneViewCameraLookAtPivot(Quaternion.Euler(-currentRot));
			}
		}

		[Shortcut("Scene View Camera - Toggle Perspective", KeyCode.Slash, ShortcutModifiers.Control)]
		public static void TogglePerspective()
		{
			SceneView.lastActiveSceneView.orthographic = !SceneView.lastActiveSceneView.orthographic;
		}

		private static void MakeSceneViewCameraLookAtPivot(Quaternion direction)
		{
			var sceneView = SceneView.lastActiveSceneView;

			if (sceneView == null)
				return;

			var camera = sceneView.camera;
			var pivot = sceneView.pivot;

			sceneView.LookAt(pivot, direction);
		}
	}
}
