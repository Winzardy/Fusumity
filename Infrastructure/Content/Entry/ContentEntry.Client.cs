namespace Content
{
	public sealed partial class ContentEntry<T>
	{
		public void SetValue(in T newValue) => ContentEditValue = newValue;
	}
}
