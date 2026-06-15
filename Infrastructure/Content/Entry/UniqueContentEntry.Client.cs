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

		public UniqueContentEntry<T> Clone()
		{
			var clone = new UniqueContentEntry<T>(in BaseValue, in Guid, Id)
			{
				Redirect = Redirect
			};
			return clone;
		}

		IUniqueContentEntry IUniqueContentEntry.Clone() => Clone();
	}

	public partial interface IUniqueContentEntry
	{
		SerializableGuid RegenerateGuid();
		void SetGuid(in SerializableGuid newGuid);
		IUniqueContentEntry Clone();
	}
}
