using System;

namespace Content
{
	public sealed partial class ContentEntry<T>
	{
		public void SetValue(in T newValue) => ContentEditValue = newValue;
	}

	public partial struct ContentEntry<T, TFilter> : IFilteredContentEntry
	{
		IContentEntry IFilteredContentEntry.Entry => entry;
		Type IFilteredContentEntry.Type => typeof(TFilter);
		void IFilteredContentEntry.SetValue(object value) => entry.SetValue((T) value);
	}

	/// <summary>
	/// Нужно в основном для редактора, чтобы ограничить выбор типов в инспекторе!
	/// </summary>
	public interface IFilteredContentEntry
	{
		public IContentEntry Entry { get; }

		public Type Type { get; }

		public void SetValue(object value);
	}
}
