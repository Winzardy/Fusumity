using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AssetManagement
{
	using UnityAssetLabelReference = UnityEngine.AddressableAssets.AssetLabelReference;

	[Serializable]
	public struct AssetLabelReference
	{
		[FormerlySerializedAs("_assetLabelReference")]
		[SerializeField]
		private UnityAssetLabelReference _reference;

		public UnityAssetLabelReference Reference { get => _reference; }
	}
}
