#if UNITY_EDITOR
namespace Content.ScriptableObjects
{
	public abstract partial class ContentDatabaseScriptableObject<T>
	{
		protected override void OnValidate()
		{
			base.OnValidate();
			_entry.scriptableObject = this;
		}
	}
}
#endif
