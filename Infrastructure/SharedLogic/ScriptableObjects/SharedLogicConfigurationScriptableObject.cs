using SharedLogic;
using UnityEngine;

namespace Content.ScriptableObjects
{
	[CreateAssetMenu(menuName = ContentMenuConstants.CREATE_MENU + "SharedLogic/Configuration", fileName = "Configuration")]
	public class SharedLogicConfigurationScriptableObject : SingleContentEntryScriptableObject<SharedLogicConfiguration>
	{
	}
}
