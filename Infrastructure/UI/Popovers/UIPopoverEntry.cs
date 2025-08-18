using System;
using Content;

namespace UI.Popovers
{
	[Serializable]
	[Constants("UI")]
	//[Documentation("https://www.notion.so/winzardy/Popup-4486acfcfe8143728a8a293d87b36193?pvs=4")]
	public struct UIPopoverEntry
	{
		public LayoutEntry<UIBasePopoverLayout> layout;
	}
}
