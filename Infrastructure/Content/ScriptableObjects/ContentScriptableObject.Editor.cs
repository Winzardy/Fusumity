#if UNITY_EDITOR
using System;
using System.Globalization;
using Sapientia.Extensions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentScriptableObject
	{
		/// <see cref="timeCreated"/>
		public const string TIME_CREATED_FILED_NAME = "timeCreated";

		/// <inheritdoc cref="CreationTimeStr"/>
		public const string CREATION_TIME_TOOLTIP = "Не всегда является временем когда был создан ассет, но стремится к этомy";

		/// <summary>
		/// Включает/выключает отображение основного Entry в инспекторе
		/// </summary>
		public virtual bool UseCustomInspector => false;

		/// <inheritdoc cref="Sync(bool)"/>
		public void Sync() => Sync(true);

		/// <summary>
		/// Синхронизирует Entry с ScriptableObject
		/// </summary>
		public void Sync(bool forceSave) => OnSync(forceSave);

		protected virtual void OnSync(bool forceSave)
		{
		}

		public virtual bool NeedSync() => false;

		protected virtual void OnValidate()
		{
			if (timeCreated != 0)
				return;

			ForceUpdateTimeCreated();
		}

		public void ForceUpdateTimeCreated()
		{
			timeCreated = DateTime.UtcNow.Ticks;
			UnityEditor.EditorUtility.SetDirty(this);
		}

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
	}
}
#endif
