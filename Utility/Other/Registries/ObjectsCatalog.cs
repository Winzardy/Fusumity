using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Fusumity.Utility
{
	public abstract class ObjectsCatalog
	{
		public abstract Type ObjectType { get; }

		public ObjectsCatalog<T> Convert<T>()
		{
			return this as ObjectsCatalog<T>;
		}
	}

	public class ObjectsCatalog<T> : ObjectsCatalog
	{
		private readonly List<T> _objects = new List<T>();

		public override Type ObjectType { get { return typeof(T); } }
		public ReadOnlyCollection<T> Objects { get; }

		public event Action Modified;
		public event Action<T> ObjectAdded;
		public event Action<T> ObjectRemoved;

		public ObjectsCatalog()
		{
			Objects = new ReadOnlyCollection<T>(_objects);
		}

		public void Add(T obj)
		{
			_objects.Add(obj);
			ObjectAdded?.Invoke(obj);
			Modified?.Invoke();
		}

		public bool Remove(T obj)
		{
			var removed = _objects.Remove(obj);
			if (removed)
			{
				ObjectRemoved?.Invoke(obj);
				Modified?.Invoke();
			}

			return removed;
		}
	}
}
