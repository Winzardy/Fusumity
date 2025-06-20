using System;

namespace Content.ScriptableObjects
{
	[Serializable, ClientOnly]
	internal sealed partial class ScriptableSingleContentEntry<T> : SingleContentEntry<T>, IScriptableContentEntry<T>
	{
		public ContentScriptableObject scriptableObject;

		public ContentScriptableObject ScriptableObject => scriptableObject;

		public override object Context => scriptableObject;

		internal ref T ScriptableEditValue => ref ContentEditValue;
		ref T IScriptableContentEntry<T>.EditValue => ref ScriptableEditValue;

		public ScriptableSingleContentEntry(in T value) : base(in value)
		{
		}
	}
}
