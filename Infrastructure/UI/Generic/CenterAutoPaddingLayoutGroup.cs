using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	[ExecuteAlways]
	[RequireComponent(typeof(HorizontalOrVerticalLayoutGroup))]
	public class CenterAutoPaddingLayoutGroup : UIBehaviour
	{
		[InfoBox("Написал на скорую руку, могут быть проблемы :)")]
		[ReadOnly]
		[SerializeField]
		private HorizontalOrVerticalLayoutGroup layoutGroup;

		[SerializeField]
		private RectTransform viewport;

		[SerializeField]
		private RectTransform content;

		public Toggle<int> minPadding;

		protected override void OnEnable()
		{
			base.OnEnable();
			UpdatePadding();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			base.OnRectTransformDimensionsChange();
			UpdatePadding();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			UpdatePadding();
		}

		private void UpdatePadding()
		{
			if (viewport == null || content == null || layoutGroup == null)
				return;

			var contentWidth = CalculateContentWidth();
			var viewportWidth = viewport.rect.width;

			var offset = Mathf.Max(0f, (viewportWidth - contentWidth) * 0.5f);

			var padding = layoutGroup.padding;
			var roundToInt = Mathf.RoundToInt(offset);

			if (minPadding)
				roundToInt = Mathf.Max(roundToInt, minPadding);

			if (layoutGroup is HorizontalLayoutGroup)
				padding.left = roundToInt;
			else
				padding.top = roundToInt;

			layoutGroup.padding = padding;
		}

		private float CalculateContentWidth()
		{
			var width = 0f;

			for (var i = 0; i < content.childCount; i++)
			{
				var child = content.GetChild(i) as RectTransform;

				if (child == null || !child.gameObject.activeSelf)
					continue;

				width += LayoutUtility.GetPreferredWidth(child);

				if (i < content.childCount - 1)
					width += layoutGroup.spacing;
			}

			return width;
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			UpdatePadding();
		}

		protected override void Reset()
		{
			base.Reset();

			ResetLayoutGroup();
		}

		[ContextMenu("Reset layout group")]
		private void ResetLayoutGroup()
		{
			layoutGroup = GetComponent<HorizontalLayoutGroup>();
		}
#endif
	}
}
