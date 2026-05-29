#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Editor;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Extensions;
using UnityEngine;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentEntryScriptableObject<T>
	{
		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// Only inspector
		/// </summary>
		private Toggle<string> _customId
		{
			get => new(useCustomId ? _entry.Id : Id, useCustomId);
			set
			{
				if (value.enable && !useCustomId)
					value.value = _entry.Id;

				useCustomId = value.enable;

				if (useCustomId)
					_entry.SetId(value);
			}
		}

		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// Only inspector
		/// </summary>
		private SerializableGuid _guid => Guid;

		protected override void OnSync(bool forceSave)
		{
			var unityGuidStr = this.ToGuid();

			if (!SerializableGuid.TryParse(unityGuidStr, out var guid))
			{
				if (!unityGuidStr.IsNullOrWhiteSpace())
					ContentDebug.LogWarning("Can't [" + unityGuidStr + "] parse guid", this);
				return;
			}

			if (ForceCreateEntry(guid))
				return;

			if (_entry.Guid == guid)
				return;

			ForceUpdateTimeCreated();
			this.RecursiveRegenerateAndRefresh(!forceSave);
			_entry.scriptableObject = this;
			_entry.SetGuid(in guid);

			ContentEditorCache.Refresh(this);
		}

		public override bool NeedSync()
		{
			var unityGuidStr = this.ToGuid();

			if (!SerializableGuid.TryParse(unityGuidStr, out var guid))
				return true;

			return _entry.Guid != guid;
		}

		public bool ForceCreateEntry(in SerializableGuid guid, string id = null)
		{
			if (_entry != null)
				return false;

			_entry = new ScriptableContentEntry<T>(default, guid)
			{
				scriptableObject = this
			};
			SetId(id);
			return true;
		}

		public void SetId(string id)
		{
			useCustomId = !id.IsNullOrEmpty();
			_entry.id   = id;
		}
	}

	public abstract partial class ContentEntryScriptableObject
	{
		/// <summary>
		/// <see cref="ContentEntryScriptableObject{T}._customId"/>
		/// </summary>
		public const string CUSTOM_ID_FIELD_NAME = "_customId";

		/// <summary>
		/// <see cref="ContentEntryScriptableObject{T}.useCustomId"/>
		/// </summary>
		public const string USE_CUSTOM_ID_FIELD_NAME = "useCustomId";

		/// <summary>
		/// <see cref="ContentEntryScriptableObject{T}._guid"/>
		/// </summary>
		public const string GUID_FIELD_NAME = "_guid";

		[ContextMenu("Content Entry/Regenerate All Guids (Recursive)", false, priority: 1100)]
		public void RecursiveRegenerateGuidAndRefresh()
		{
			this.RecursiveRegenerateAndRefresh();
		}

		[ContextMenu("Content Entry/Regenerate All Guids (Recursive)", true)]
		public bool RecursiveRegenerateGuidAndRefreshValidate() => ContentEntryDebugModeMenu.IsEnable;
	}

	public partial interface IContentScriptableObject
	{
		public long TimeCreated { get; }
	}
}
#endif
