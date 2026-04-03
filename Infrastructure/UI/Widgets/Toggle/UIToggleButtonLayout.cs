using ZenoTween;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Fusumity.MVVM.UI;

namespace UI
{
	public class UIToggleButtonLayout : UILabeledButtonLayout
	{
		public TMP_Text sublabel;
		public UIAttentionIndicatorLayout indicator;

		[ReadOnly]
		[InfoBox("Используйте ActivitySwitcher", InfoMessageType.Warning)]
		[FoldoutGroup("Toggle Animations")]
		[HorizontalGroup("Toggle Animations/OnOff"), LabelText("On")]
		[SerializeReference]
		public SequenceParticipant onSequence;

		[ReadOnly]
		[InfoBox("Используйте ActivitySwitcher", InfoMessageType.Warning)]
		[HorizontalGroup("Toggle Animations/OnOff"), LabelText("Off")]
		[SerializeReference]
		public SequenceParticipant offSequence;

		[Space]
		public StateSwitcher<bool> activitySwitcher;

		protected internal override void OnValidate()
		{
			if (Application.isPlaying)
				return;

			onSequence?.Validate(gameObject);
			offSequence?.Validate(gameObject);
		}
	}
}
