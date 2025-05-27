#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Editor;
using Fusumity.Editor.Utility;
using Sapientia;
using Sapientia.Extensions;
using UnityEditor;

namespace Content.ScriptableObjects
{
	public abstract partial class ContentEntryScriptableObject<T>
	{
		// ReSharper disable once StaticMemberInGenericType
		private static readonly HashSet<string> _firstSkip = new();

		// ReSharper disable once InconsistentNaming
		/// <summary>
		/// Only inspector
		/// </summary>
		private Toggle<string> _customId
		{
			get => new
				(useCustomId ? _entry.id : Id, useCustomId);
			set
			{
				if (value.enable && !useCustomId)
					value.value = _entry.id;

				useCustomId = value.enable;

				if (useCustomId)
					_entry.id = value;
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

			if (_entry == null)
			{
				_entry = new ScriptableContentEntry<T>(default, in guid)
				{
					scriptableObject = this
				};
				return;
			}

			if (_entry.Guid == guid)
				return;

			ForceUpdateTimeCreated();
			this.RecursiveRegenerateAndRefresh(false);
			_entry.scriptableObject = this;
			_entry.SetGuid(in guid);

			if (!forceSave)
				return;

			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssetIfDirty(this);
			AssetDatabase.Refresh();
		}

		public override bool NeedSync()
		{
			var unityGuidStr = this.ToGuid();

			if (!SerializableGuid.TryParse(unityGuidStr, out var guid))
				return true;

			return _entry.Guid != guid;
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
	}

	public partial interface IContentScriptableObject
	{
		public long TimeCreated { get; }
	}
}
#endif
