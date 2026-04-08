using System;
using System.Collections.Generic;

namespace Fusumity.Utility
{
	public interface IClickable
	{
		event Action Clicked;
	}

	public interface IClickable<T>
	{
		event Action<T> Clicked;
	}

	public static class ClickableExtensions
	{
		public static void Subscribe<T>(this IEnumerable<IClickable<T>> collection, Action<T> onClick)
		{
			foreach (var clickable in collection)
				clickable.Clicked += onClick;
		}

		public static void Unsubscribe<T>(this IEnumerable<IClickable<T>> collection, Action<T> onClick)
		{
			foreach (var clickable in collection)
				clickable.Clicked -= onClick;
		}
	}
}
