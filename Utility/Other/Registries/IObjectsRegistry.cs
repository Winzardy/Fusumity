using System;
using System.Collections.ObjectModel;

namespace Fusumity.Utility
{
	public interface IObjectsRegistry
	{
		public void Register<T>(T obj);
		public bool Unregister<T>(T obj);
		public void RegisterAfter<T>(T obj, Func<bool> predicate);
		public void AddCatalog(ObjectsCatalog catalog);

		public ObjectsCatalog<T> GetCatalog<T>();
		public ReadOnlyCollection<T> GetObjects<T>();
	}
}
