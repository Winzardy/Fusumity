using UI;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	public abstract class UIViewCollectionLayout<TViewLayout> : UIBaseLayout
	{
		public TViewLayout template;
		public RectTransform root;
	}
}
