using System;
using Sapientia;
using Sapientia.Extensions;

namespace Content.ScriptableObjects
{
	[Serializable, ClientOnly]
	internal sealed partial class ScriptableContentEntry<T> : UniqueContentEntry<T>, IScriptableContentEntry<T>, IIdentifiable
	{
		public ContentScriptableObject scriptableObject;

		public override bool IsValid() => Guid != SerializableGuid.Empty;
		public ContentScriptableObject ScriptableObject => scriptableObject;
		public override object Context => scriptableObject;

		internal ref T ScriptableEditValue => ref ContentEditValue;
		ref T IScriptableContentEntry<T>.EditValue => ref ScriptableEditValue;

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
		internal ref T EditValue { get; }
	}

	public partial interface IScriptableContentEntry : IContentEntry
	{
		/// <summary>
		/// Только в билде!
		/// </summary>
		public ContentScriptableObject ScriptableObject { get; }
	}
}
