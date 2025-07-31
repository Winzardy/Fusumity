using Content;
using Sirenix.OdinInspector;
using UnityEngine;

namespace InAppPurchasing
{
	public abstract partial class IAPProductEntry
	{
		// TODO: MetaData?
		[ClientOnly]
		public string titleLocKey;
		[ClientOnly]
		public string descriptionLocKey;
		[ClientOnly, Tooltip("Используется в основном для фейка"), PropertySpace(0, 10)]
		public string price;
	}
}
