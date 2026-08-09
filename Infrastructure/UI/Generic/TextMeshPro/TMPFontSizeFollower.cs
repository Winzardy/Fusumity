using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	[ExecuteAlways, RequireComponent(typeof(TMP_Text))]
	public class TMPFontSizeFollower : ReactiveBehaviour
	{
		[SerializeField, ReadOnly]
		private TMP_Text _self;

		[SerializeField]
		private TMP_Text _target;

		private bool _subscribed;

		protected override UpdateMode Mode { get => UpdateMode.LateUpdate; }

		protected override void OnLateUpdate()
		{
			if (!_target)
				return;
			_self.fontSize = _target.fontSize;
		}

		private void Reset()
		{
			_self = GetComponent<TMP_Text>();
		}
	}
}
