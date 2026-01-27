using Game.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace UI
{
	public class UIPricedButtonLayout : UIStatefulButtonLayout
	{
		[Space]
		[NotNull]
		public UILabeledIconCollectionLayout prices;

		public UILabeledIconLayout primaryPrice;
	}
}
