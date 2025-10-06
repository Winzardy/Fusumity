using System;

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
}
