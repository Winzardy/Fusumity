using UnityEngine;

namespace UI
{
	public interface IOrderedLayoutElement
	{
		bool ignoreLayout { get; }
		GameObject gameObject { get; }
		void SetOrder(int order, int total);
	}

	public static class OrderedLayoutElementExtensions
	{
		public static bool IsNull(this IOrderedLayoutElement element)
		{
			if (element == null)
				return true;

			if (element is MonoBehaviour monoBehaviour
				&& monoBehaviour == null)
				return true;

			return false;
		}
	}
}
