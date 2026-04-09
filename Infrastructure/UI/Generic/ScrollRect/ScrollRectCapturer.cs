using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(ScrollRect))]
	public class ScrollRectCapturer : MonoBehaviour
	{
		[ReadOnly]
		public ScrollRect scrollRect;

		[ShowInInspector]
		[ReadOnly]
		[NonSerialized]
		private Vector2 _normalizedPosition;

		public void CaptureNormalizedPosition()
		{
			SetNormalizePosition(scrollRect.normalizedPosition);
		}

		public void SetNormalizePosition(Vector2 normalizedPosition)
		{
			_normalizedPosition = normalizedPosition;
		}

		public Vector2 GetNormalizedPosition() => _normalizedPosition;

#if UNITY_EDITOR
		protected void Reset()
		{
			scrollRect = GetComponent<ScrollRect>();
		}
#endif
	}
}
