using System;
using Content;
using Sapientia;
using UnityEngine;

namespace UI.Popups
{
	[Serializable]
	[Constants("UI")]
	[Documentation("https://www.notion.so/winzardy/Popup-4486acfcfe8143728a8a293d87b36193?pvs=4")]
	public struct UIPopupConfig
	{
		public LayoutEntry<UIBasePopupLayout> layout;

		[Space]
		public Toggle<int> poolCapacity;
	}
}
