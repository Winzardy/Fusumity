using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Scroll.FlickSnap
{
	[RequireComponent(typeof(UIScrollLayout))]
	public abstract class UIScrollLayoutComponent : MonoBehaviour
	{
		[ReadOnly, PropertySpace(0, 10)]
		[SerializeField]
		protected UIScrollLayout _layout;

		private void Reset()
		{
			_layout = GetComponent<UIScrollLayout>();
		}
	}
}
