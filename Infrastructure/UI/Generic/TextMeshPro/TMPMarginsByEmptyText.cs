using Sapientia.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
	[ExecuteAlways, RequireComponent(typeof(TMP_Text))]
	public class TMPMarginsByEmptyText : ReactiveBehaviour
	{
		[SerializeField, ReadOnly]
		private TMP_Text _self;

		[SerializeField]
		private Vector4 _normal;

		[SerializeField]
		private Vector4 _onEmpty;

		protected override UpdateMode Mode { get => UpdateMode.LateUpdate; }

		protected override void OnLateUpdate()
		{
			if (!_self)
				return;

			_self.margin = _self.text.IsNullOrEmpty() ? _onEmpty : _normal;
		}

		private void Reset()
		{
			_self = GetComponent<TMP_Text>();
		}
	}
}
