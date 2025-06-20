using System.Collections.Generic;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
	public enum RadialLayoutGroupAlignment
	{
		Top = 0,
		Middle = 1,
		Bottom = 2,
	}

	[HideMonoScript]
	[AddComponentMenu("Layout/Radial Layout Group", 152)]
	public class RadialLayoutGroup : LayoutGroup
	{
		public RadialLayoutGroupAlignment alignment;

		[Tooltip("Отступ от центра"), LabelText("Padding")]
		[FormerlySerializedAs("padding")]
		public float radialPadding;

		[Unit(Units.Radian, Units.Degree), SuffixLabel("@RadialLayoutGroup.DisplayRad($value)", true), ShowInInspector]
		public float startAngle;

		public bool useItemSize = true;

		[Unit(Units.Radian, Units.Degree), SuffixLabel("@RadialLayoutGroup.DisplayRad($value)", true), ShowInInspector]
		public float angleSpacing;

		[Tooltip("Размещает элементы равноудалено, но так же работает наложение Spacing и AngleSpacing")]
		public bool equalSpacing;

		[HideIf(nameof(equalSpacing))]
		public float spacing;

		[Tooltip("Может пригодиться для анимаций)")]
		public float spacingFactor = 1;

		[Tooltip("Аналог Reverse")]
		public bool clockwise;

		protected override void OnEnable()
		{
			base.OnEnable();


#if UNITY_EDITOR
			if (Application.isPlaying)
				CalculateLayoutInput();
			else
				EditorApplication.delayCall += CalculateLayoutInput;
#else
			CalculateLayoutInput();
#endif
		}

		public override void CalculateLayoutInputVertical() => CalculateLayoutInput();

		public override void CalculateLayoutInputHorizontal() => CalculateLayoutInput();

		private void CalculateLayoutInput()
		{
			m_Tracker.Clear();

			if (this == null)
				return;

			if (transform.childCount == 0)
				return;

			using (ListPool<RectTransform>.Get(out var children))
			{
				Fill(children, out var childrenCount);

				var groupRect = rectTransform.rect;
				var groupMaxDimension = groupRect.width.Max(groupRect.height);

				var angle = startAngle;

				foreach (var (child, index) in children.WithIndexSafe())
				{
					var childRect = child.rect;
					var childMaxDimension = childRect.height.Max(childRect.width);

					//Угол между элементами в радианах
					var a = 0f;

					if (equalSpacing)
						a += Mathf.PI * 2 / childrenCount;
					else
						a += Mathf.Asin((childMaxDimension + spacing + (useItemSize ? 0 : -childMaxDimension)) / groupMaxDimension) * 2;

					a += angleSpacing;

					switch (alignment)
					{
						case RadialLayoutGroupAlignment.Top:
						case RadialLayoutGroupAlignment.Middle:
							if (index > 0)
								angle -= spacingFactor * a;
							break;

						case RadialLayoutGroupAlignment.Bottom:
							if (index > 0)
								angle += spacingFactor * a;
							break;
					}

					var totalAngle = angle;

					if (alignment == RadialLayoutGroupAlignment.Middle && childrenCount > 1)
					{
						var y = ((childrenCount - 1) / 2f) * a;
						totalAngle = angle + y;
					}

					var normalizedPosition = new Vector3 {x = Mathf.Cos(totalAngle), y = Mathf.Sin(totalAngle)};

					var localPosition = new Vector3
					{
						x = normalizedPosition.x * groupRect.width / 2,
						y = normalizedPosition.y * groupRect.height / 2
					};
					localPosition += normalizedPosition * radialPadding;

					children[index].localPosition = localPosition;

					child.anchorMin = child.anchorMax = child.pivot = Vector2MathUtility.center;

					m_Tracker.Add(this, child,
						DrivenTransformProperties.Anchors |
						DrivenTransformProperties.AnchoredPosition |
						DrivenTransformProperties.Pivot);
				}
			}
		}

		private void Fill(List<RectTransform> children, out int count)
		{
			count = 0;
			for (int i = 0; i < transform.childCount; i++)
			{
				var index = clockwise ? transform.childCount - 1 - i : i;
				var child = transform.GetChild(index);

				if (!child.gameObject.IsActive())
					continue;

				if (child.TryGetComponent(out RectTransform childRectTransform))
				{
					if (childRectTransform.TryGetComponent(out LayoutElement layoutElement))
					{
						if (layoutElement.ignoreLayout)
							continue;
					}

					count++;
					children.Add(childRectTransform);
				}
			}
		}

		public override void SetLayoutHorizontal()
		{
		}

		public override void SetLayoutVertical()
		{
		}

#if UNITY_EDITOR

		protected override void OnValidate()
		{
#if UNITY_EDITOR
			EditorApplication.delayCall += CalculateLayoutInput;
#endif
			childAlignment = TextAnchor.LowerCenter;
		}

		public RectTransform RectTransformEditor => rectTransform;
#endif

		public static string DisplayRad(float value) => $"{value:F2} rad";
	}
}
