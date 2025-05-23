namespace Content.ScriptableObjects
{
	public abstract partial class SingleContentEntryScriptableObject<T>
	{
		protected override void OnValidate()
		{
			base.OnValidate();

			_entry.scriptableObject = this;
		}
	}
}
