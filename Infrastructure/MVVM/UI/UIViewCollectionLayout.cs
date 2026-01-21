using System.Diagnostics.CodeAnalysis;
using UI;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	public abstract class UIViewCollectionLayout<TViewLayout> : UIBaseLayout
	{
		[NotNull]
		public RectTransform root;
		[NotNull]
		public TViewLayout template;
	}
}
