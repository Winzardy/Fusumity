using ZenoTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
	public class UIToggleButtonLayout : UILabeledButtonLayout
	{
		[FoldoutGroup("Toggle Animations")]
		[HorizontalGroup("Toggle Animations/OnOff"), LabelText("On")]
		[SerializeReference]
		public SequenceParticipant onSequence;

		[HorizontalGroup("Toggle Animations/OnOff"), LabelText("Off")]
		[SerializeReference]
		public SequenceParticipant offSequence;

		[Space]
		public StateSwitcher<bool> activitySwitcher;

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			onSequence?.Validate(gameObject);
			offSequence?.Validate(gameObject);
		}
	}
}
