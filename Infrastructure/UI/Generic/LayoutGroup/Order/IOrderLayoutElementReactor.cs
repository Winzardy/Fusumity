using UnityEngine;

namespace UI
{
	public interface IOrderedLayoutElementReactor
	{
		void OnOrderChanged(int index);
	}

	public abstract class OrderedLayoutElementReactor : MonoBehaviour, IOrderedLayoutElementReactor
	{
		public abstract void OnOrderChanged(int index);
	}
}
