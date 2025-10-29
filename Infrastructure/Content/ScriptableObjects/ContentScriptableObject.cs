using Sapientia.Extensions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject : ScriptableObject, IContentScriptableObject
	{
		[SerializeField]
		protected long timeCreated;

		public virtual IContentEntry Import(bool clone) => null;

		/// <summary>
		/// Используется ли контент? Если нет, то при обновлении базы пропустит ScriptableObject
		/// </summary>
		public virtual bool Enabled => true;

		public string techDescription;

		public void SyncedUpdate() => OnUpdated();

		protected virtual void OnUpdated()
		{
		}

		public override string ToString() => $"[ 	<b>{name}</b>	 ]	(type: {GetType().Name})";

#if UNITY_EDITOR
		private bool _useTechDescription;

		[ContextMenu("Tech Description/Enable")]
		public void EnableTechDescription() => _useTechDescription = true;

		[ContextMenu("Tech Description/Disable")]
		public void DisableTechDescription() => _useTechDescription = false;

		[ContextMenu("Tech Description/Enable", true)]
		public bool EnableTechDescriptionValidate() => !_useTechDescription;

		[ContextMenu("Tech Description/Disable", true)]
		public bool DisableTechDescriptionValidate() => _useTechDescription;

		private bool ShowTechDescriptionEditor => !techDescription.IsNullOrEmpty() || _useTechDescription;
#endif
	}
}
