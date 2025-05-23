using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject : ScriptableObject, IContentScriptableObject
	{
		[SerializeField]
		protected long timeCreated;

		/// <summary>
		/// Используется ли контент? Если нет, то при обновлении базы пропустит ScriptableObject
		/// </summary>
		public virtual bool Enabled => true;

		public override string ToString() => $"[ 	<b>{name}</b>	 ]	(type: {GetType().Name})";
	}
}
