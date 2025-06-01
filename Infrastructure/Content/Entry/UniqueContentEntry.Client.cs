namespace Content
{
	public partial class UniqueContentEntry<T>
	{
		public virtual void RegenerateGuid() => SetGuid(SerializableGuid.New());

		public void SetGuid(in SerializableGuid newGuid) => guid = newGuid;
	}

	public partial interface IUniqueContentEntry
	{
		public void RegenerateGuid();
		public void SetGuid(in SerializableGuid newGuid);
	}
}
