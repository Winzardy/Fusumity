namespace Content
{
	public partial class UniqueContentEntry<T>
	{
		public virtual void RegenerateGuid() => SetGuid(SerializableGuid.New());

		public void SetGuid(in SerializableGuid newGuid) => guid = newGuid;

		public UniqueContentEntry<T> Clone() => new(in Value, in Guid);

		IUniqueContentEntry IUniqueContentEntry.Clone() => Clone();
	}

	public partial interface IUniqueContentEntry
	{
		public void RegenerateGuid();
		public void SetGuid(in SerializableGuid newGuid);
		public IUniqueContentEntry Clone();
	}
}
