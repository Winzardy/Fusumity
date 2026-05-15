using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AssetManagement
{
	[Serializable]
	public class AssetLabelRef
	{
		[SerializeField]
		private AssetLabelReference _assetLabelReference;

		public AssetLabelReference AssetLabelReference => _assetLabelReference;
	}
}
