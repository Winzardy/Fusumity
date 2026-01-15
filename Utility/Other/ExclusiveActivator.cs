using System;
using System.Collections.Generic;

namespace Fusumity.Utility
{
	public interface IExclusiveActivated
	{
		int Priority { get; }
		void SetActive(bool active);
	}

	public class ExclusiveActivator : ExclusiveActivator<IExclusiveActivated> { }
	public class ExclusiveActivator<T> where T : class, IExclusiveActivated
	{
		private List<T> _objects = new List<T>();

		public T CurrentlyActive { get; private set; }

		public event Action<T> CurrentlyActiveChanged;

		public void Add(T obj)
		{
			_objects.Add(obj);
			Reevaluate(obj);
		}

		public void Remove(T obj)
		{
			_objects.Remove(obj);
			Reevaluate();
		}

		private void Reevaluate(T focused = null)
		{
			var highest = focused;

			for (int i = 0; i < _objects.Count; i++)
			{
				var obj = _objects[i];

				if (highest == null || highest.Priority < obj.Priority)
					highest = obj;
			}

			if (CurrentlyActive == highest)
				return;

			CurrentlyActive?.SetActive(false);
			CurrentlyActive = highest;
			CurrentlyActive?.SetActive(true);

			CurrentlyActiveChanged?.Invoke(CurrentlyActive);
		}
	}
}
