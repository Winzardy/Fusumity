using System;
using Sapientia;
using Sapientia.Extensions;

namespace Content.ScriptableObjects
{
	[Serializable]
	internal sealed partial class ScriptableContentEntry<T> : UniqueContentEntry<T>, IScriptableContentEntry<T>, IIdentifiable
	{
		public ContentScriptableObject scriptableObject;

		public string id;

		public override string Id => id.IsNullOrEmpty() ? base.Id : id;

		public override bool IsValid() => Guid != SerializableGuid.Empty;

		public ContentScriptableObject ScriptableObject => scriptableObject;
		public override object Context => scriptableObject;

		public ScriptableContentEntry(in T value, in SerializableGuid guid) : base(in value, in guid)
		{
		}

		public override void RegenerateGuid()
		{
			// Guid присваивается от ScriptableObject
			// Поэтому задать новый Guid для ScriptableContentEntry нельзя!
		}

		public override string ToString() => id.IsNullOrEmpty() ? base.ToString() : id;
	}

	public interface IScriptableContentEntry<T> : IContentEntry<T>, IScriptableContentEntry
	{
	}

	public partial interface IScriptableContentEntry : IContentEntry
	{
		/// <summary>
		/// <see cref="ScriptableContentEntry{T}.scriptableObject"/>
		/// </summary>
		public const string SCRIPTABLEOBJECT_FIELD_NAME = "scriptableObject";

		/// <summary>
		/// <see cref="ScriptableContentEntry{T}.id"/>
		/// </summary>
		public const string ID_FIELD_NAME = "id";

		/// <summary>
		/// Только в билде!
		/// </summary>
		public ContentScriptableObject ScriptableObject { get; }
	}
}
