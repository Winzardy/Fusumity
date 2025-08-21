using System;
using Content;

namespace UI.Windows
{
	[Serializable]
	[Constants("UI")]
	[Documentation("https://www.notion.so/winzardy/Window-8f15112b45b24c43b35f72f40dd4771d?pvs=4")]
	public struct UIWindowEntry
	{
		public LayoutEntry<UIBaseWindowLayout> layout;
	}
}
