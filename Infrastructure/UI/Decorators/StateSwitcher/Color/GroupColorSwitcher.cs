using UnityEngine;

namespace UI
{
	public class GroupColorSwitcher : GroupStateSwitcher<Color>
	{
		[ContextMenu("Force Set Current")]
		private void ForceSetCurrent() => Switch(current);
	}
}
