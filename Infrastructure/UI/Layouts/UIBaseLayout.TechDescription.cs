#if UNITY_EDITOR
using UnityEngine;

namespace UI
{
	public abstract partial class UIBaseLayout
	{

		private bool _useTechDescription;

		/// <summary>
		/// Включён ли режим редактирования <see cref="techDescription"/>. Если нет — поле в инспекторе read-only.
		/// </summary>
		public bool UseTechDescriptionEditor => _useTechDescription;

		public void SetUseTechDescriptionEditor(bool value) => _useTechDescription = value;

		[ContextMenu("Tech Description/Enable")]
		private void EnableTechDescription() => _useTechDescription = true;

		[ContextMenu("Tech Description/Disable")]
		private void DisableTechDescription() => _useTechDescription = false;

		[ContextMenu("Tech Description/Enable", true)]
		private bool EnableTechDescriptionValidate() => !_useTechDescription;

		[ContextMenu("Tech Description/Disable", true)]
		private bool DisableTechDescriptionValidate() => _useTechDescription;
	}
}
#endif
