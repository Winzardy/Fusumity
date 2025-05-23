using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIProgressBarLayout : UIBaseLayout
	{
		public enum Type
		{
			Image,
			ScrollBar
		}

		public Type type;

		[ShowIf(nameof(type), Type.Image)]
		public Image image;

		[ShowIf(nameof(type), Type.ScrollBar)]
		public Scrollbar scrollBar;

		[TitleGroup("Animation")]
		[LabelText("Duration")]
		public float animationDuration = 0.75f;

		[LabelText("Ease")]
		public Ease animationEase = Ease.OutCubic;

		[Tooltip("Скрывать бар вне анимации, решает проблему например с trace баром (след)")]
		public bool hideOutsideAnimation;

		protected override void Reset()
		{
			base.Reset();

			image = GetComponentInChildren<Image>();
			scrollBar = GetComponentInChildren<Scrollbar>();
		}

		protected internal override void OnValidate()
		{
			base.OnValidate();

			if (type != Type.Image)
				return;
			if (image == null)
				return;

			if (image.type != Image.Type.Filled)
				GUIDebug.LogError("Target Image is not of type Filled!", image);
		}
	}
}
