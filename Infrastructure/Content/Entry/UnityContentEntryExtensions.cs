using JetBrains.Annotations;

namespace Content
{
	using UnityObject = UnityEngine.Object;

	public static class UnityContentEntryExtensions
	{
		[CanBeNull]
		public static UnityObject ToDebugContext<T>(this in ContentReference<T> reference)
		{
			if (!reference.IsValid())
				return null;

			return reference.GetEntry()
				.Context as UnityObject;
		}
	}
}
