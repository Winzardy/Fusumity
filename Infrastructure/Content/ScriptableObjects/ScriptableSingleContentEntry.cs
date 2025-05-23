using System;

namespace Content.ScriptableObjects
{
	[Serializable]
	internal sealed partial class ScriptableSingleContentEntry<T> : SingleContentEntry<T>, IScriptableContentEntry<T>
	{
		public ContentScriptableObject scriptableObject;

		public ContentScriptableObject ScriptableObject => scriptableObject;

		public override object Context => scriptableObject;

		public ScriptableSingleContentEntry(in T value) : base(in value)
		{
		}
	}
}
