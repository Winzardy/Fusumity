using Advertising;
using Fusumity.Collections;
using Targeting;
using Sapientia.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.Advertising
{
	[CreateAssetMenu(menuName = ContentAdvertisingEditorConstants.CREATE_MENU + "Placement/Rewarded",
		fileName = "Ad_Placement_Rewarded_New")]
	public class RewardedAdPlacementScriptableObject : ContentEntryScriptableObject<RewardedAdPlacementEntry>
	{
		[Space, DictionaryDrawerSettings(KeyLabel = "Platform", ValueLabel = "Name")]
		public SerializableDictionary<PlatformEntry, string> platformToName;

		protected override void OnImport(ref RewardedAdPlacementEntry placement)
		{
			placement.platformToName = platformToName;
		}
	}
}
