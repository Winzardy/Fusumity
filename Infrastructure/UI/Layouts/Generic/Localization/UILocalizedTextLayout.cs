using System;
using TMPro;
using UnityEngine.Serialization;

namespace UI
{
	[Obsolete("Не использовать, если нужно есть TMPLocalizer")]
	public class UILocalizedTextLayout : UILocalizedBaseLayout
	{
		[FormerlySerializedAs("placeholder")]
		public TMP_Text label;

		public override TMP_Text Label => label;

		protected override void Reset()
		{
			base.Reset();
			label = GetComponent<TMP_Text>();
		}
	}
}
