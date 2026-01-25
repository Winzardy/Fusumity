using System;
using Content;

namespace UI.Screens
{
	[Serializable]
	[Constants("UI")]
	[Documentation("https://www.notion.so/winzardy/Screen-d93ff2905584402fbaaeb55d06e3276e?pvs=4")]
	public struct UIScreenConfig
	{
		public UILayoutEntry<UIBaseScreenLayout> layout;
	}
}
