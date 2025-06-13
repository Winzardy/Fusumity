namespace Content
{
	public partial class SingleContentEntry<T>
	{
		public SingleContentEntry<T> Clone() => new(in Value);
	}
}
