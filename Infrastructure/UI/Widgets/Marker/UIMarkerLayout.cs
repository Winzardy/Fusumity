using System;
using Fusumity.Utility;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class UIMarkerLayout : UIBaseLayout
	{
		[Space]
		public CanvasGroup canvasGroup;

		[Space]
		public RectTransform arrow;

		public RectTransform pivot;

		[Space]
		public StateSwitcher<bool> offscreenStateSwitcher;

		[Space]
		public UIBaseLayout nested;

		[TitleGroup("Animation"), ShowIf(nameof(canvasGroup), null)]
		public float showingDuration = 0.2f;

		[ShowIf(nameof(canvasGroup), null)]
		public float hidingDuration = 0.15f;

#if UNITY_EDITOR
		[NonSerialized]
		public Vector3 gizmoWorldPosition;

		[NonSerialized]
		public Vector3 gizmoWorldOffsetPosition;

		private void OnDrawGizmos()
		{
			var origin = Gizmos.color;
			{
				if (gizmoWorldOffsetPosition != Vector3.zero)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawWireSphere(gizmoWorldPosition, 0.15f);
					Gizmos.DrawLine(gizmoWorldPosition, gizmoWorldPosition + gizmoWorldOffsetPosition);
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(gizmoWorldPosition + gizmoWorldOffsetPosition, 0.1f);
				}
				else
				{
					Gizmos.color = Color.red;
					Gizmos.DrawWireSphere(gizmoWorldPosition, 0.1f);
				}
			}
			Gizmos.color = origin;
		}
#endif
	}
}
