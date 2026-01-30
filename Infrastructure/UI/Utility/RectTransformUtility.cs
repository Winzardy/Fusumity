using System;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public static class RectTransformUtility
	{
		public static void ForceRebuild(this RectTransform rect, in int delayMs = 10, Action callback = null) =>
			ForceRebuildAsync(rect, null, delayMs, callback).Forget();

		/// <summary>
		/// Метод вызывает ForceRebuildLayoutImmediate с задержкой (delay)
		/// Основной кейс использования вложенные Layout Group
		///
		/// Использовать в супер редких случаях, является костылем...
		/// </summary>
		public static void ForceRebuild(this RectTransform rect, Func<bool> activity, in int delayMs = 10, Action callback = null) =>
			ForceRebuildAsync(rect, activity, delayMs, callback).Forget();

		private static async UniTask ForceRebuildAsync(RectTransform rect, Func<bool> activity, int delayMs = 10, Action callback = null)
		{
			Rebuild(rect, activity);
			var time = Time.realtimeSinceStartup;
			await UniTask.NextFrame();
			var diffMs = (time - Time.realtimeSinceStartup).ToMilliseconds();
			if (diffMs < delayMs)
				await UniTask.Delay(delayMs - diffMs);

			if (rect)
			{
				Rebuild(rect, activity);

				if (GUIDebug.Logging.RectTransform.rebuilt)
					GUIDebug.Log($"Forced rebuilt [ {rect.name} ]", rect);
			}

			callback?.Invoke();
		}

		private static void Rebuild(RectTransform rect, Func<bool> activity)
		{
			var active = rect.gameObject.activeSelf;
			rect.SetActive(false);
			rect.SetActive(true);

			LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

			rect.SetActive(activity?.Invoke() ?? active);
		}

		public static RectTransform FirstOrDefault(this RectTransform parent, string name)
		{
			foreach (RectTransform child in parent)
			{
				if (child.name == name)
					return child;

				var first = child.FirstOrDefault(name);
				if (first)
					return first;
			}

			return null;
		}

		public static Rect ToScreenSpace(this RectTransform rect)
		{
			var size = Vector2.Scale(rect.rect.size, rect.lossyScale);
			return new Rect((Vector2) rect.position - (size * 0.5f), size);
		}

		public static float GetMaxRectSide(this RectTransform transform)
			=> transform.rect.GetMaxRectSide();

		public static float GetMinRectSide(this RectTransform transform)
			=> transform.rect.GetMinRectSide();

		public static void SetAnchorPosition(this RectTransform rectTransform, in Vector2 anchorPosition)
		{
			rectTransform.SetAnchorPosition(anchorPosition, rectTransform.rect.size);
		}

		public static void SetAnchorPosition(this RectTransform rectTransform, in Vector2 anchorPosition, in float size)
		{
			rectTransform.SetAnchorPosition(anchorPosition, new Vector2(size, size));
		}

		public static void SetAnchorPosition(this RectTransform rectTransform, in Vector2 anchorPosition, in float sizeX, in float sizeY)
		{
			rectTransform.SetAnchorPosition(anchorPosition, new Vector2(sizeX, sizeY));
		}

		public static void SetAnchorPosition(this RectTransform rectTransform, in Vector2 anchorPosition, in Vector2 size)
		{
			rectTransform.anchorMin = anchorPosition;
			rectTransform.anchorMax = anchorPosition;
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.sizeDelta = size;
		}

		public static void SetLocalPositionZ(this RectTransform rectTransform, in float zPosition)
		{
			var position = rectTransform.localPosition;
			position.z = zPosition;
			rectTransform.localPosition = position;
		}

		public static void ResetSafe(this RectTransform rectTransform)
		{
			if (!rectTransform)
				return;

			Reset(rectTransform);
		}

		public static void Reset(this RectTransform rectTransform)
		{
			rectTransform.ResetTransform();
		}

		public static void CopyFrom(this RectTransform target, RectTransform source)
		{
			target.anchorMin = source.anchorMin;
			target.anchorMax = source.anchorMax;
			target.anchoredPosition = source.anchoredPosition;
			target.sizeDelta = source.sizeDelta;
			target.pivot = source.pivot;

			target.localRotation = source.localRotation;
			target.localScale = source.localScale;
			target.localPosition = source.localPosition;
		}

		public static void StretchHorizontally(this RectTransform rect)
		{
			rect.anchorMin = new Vector2(0, rect.anchorMin.y);
			rect.anchorMax = new Vector2(1, rect.anchorMax.y);
			rect.offsetMin = new Vector2(0, rect.offsetMin.y);
			rect.offsetMax = new Vector2(0, rect.offsetMax.y);
		}

		public static void StretchVertically(this RectTransform rect)
		{
			rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
			rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
			rect.offsetMin = new Vector2(rect.offsetMin.x, 0);
			rect.offsetMax = new Vector2(rect.offsetMax.x, 0);
		}

		public static void StretchAllSides(this RectTransform rect)
		{
			rect.anchorMin = Vector3.zero;
			rect.anchorMax = Vector3.one;
			rect.offsetMin = Vector2.zero;
			rect.offsetMax = Vector2.zero;
		}
	}
}
