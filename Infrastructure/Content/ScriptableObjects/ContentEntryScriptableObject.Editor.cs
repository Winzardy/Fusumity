#if UNITY_EDITOR
using System.Collections.Generic;
using Content.Editor;
using Sapientia;
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

		private void OnEnable()
		{
			ForceUpdateEntry();
		}

		//TODO: вызывается при ренейме?
		private void ForceUpdateEntry(bool skip = true)
		{
			var path = AssetDatabase.GetAssetPath(this);
			var unityGuidStr = AssetDatabase.AssetPathToGUID(path);

			//Вызывается два раза и второй раз валидно все, а первый нет...
			if (skip && _firstSkip.Add(unityGuidStr))
				return;

			if (!SerializableGuid.TryParse(unityGuidStr, out var guid))
			{
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

			_entry.scriptableObject = this;
			_entry.SetGuid(in guid);
			ForceUpdateTimeCreated();

			if (skip)
				this.RecursiveRegenerateAndRefresh();
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			if (_entry.Guid == SerializableGuid.Empty)
				ForceUpdateEntry(false);

			_entry.scriptableObject = this;
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
