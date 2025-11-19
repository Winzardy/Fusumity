using System;
using Content;
using Sapientia;
using UnityEngine;

namespace UI.Popovers
{
	[Serializable]
	[Constants("UI")]
	//[Documentation("https://www.notion.so/winzardy/Popup-4486acfcfe8143728a8a293d87b36193?pvs=4")]
	public struct UIPopoverConfig
	{
		public LayoutEntry<UIBasePopoverLayout> layout;

		[Space]
		public Toggle<int> poolCapacity;
	}
}
