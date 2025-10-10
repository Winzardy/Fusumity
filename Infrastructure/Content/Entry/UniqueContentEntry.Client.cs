namespace Content
{
	public partial class UniqueContentEntry<T>
	{
		public virtual SerializableGuid RegenerateGuid()
		{
			var serializableGuid = SerializableGuid.New();
			SetGuid(serializableGuid);
			return serializableGuid;
		}

		public void SetGuid(in SerializableGuid newGuid) => guid = newGuid;

		public UniqueContentEntry<T> Clone() => new(in Value, in Guid, Id);

		IUniqueContentEntry IUniqueContentEntry.Clone() => Clone();
	}

	public partial interface IUniqueContentEntry
	{
		public SerializableGuid RegenerateGuid();
		public void SetGuid(in SerializableGuid newGuid);
		public IUniqueContentEntry Clone();
	}
}
