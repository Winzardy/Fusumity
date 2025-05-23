using Advertising;
using Distribution;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.Advertising
{
	//[Constants]
	[CreateAssetMenu(menuName = ContentAdvertisingEditorConstants.CREATE_MENU + "Placement/Interstitial",
		fileName = "Ad_Placement_Interstitial_New")]
	public class InterstitialAdPlacementScriptableObject : ContentEntryScriptableObject<InterstitialAdPlacementEntry>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Platform", ValueLabel = "Name")]
		public SerializableDictionary<PlatformEntry, string> platformToName;

		protected override void OnImport()
		{
			Value.platformToName = platformToName;
		}
	}
}
