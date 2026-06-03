using System;
using Content;
using UnityEngine;

namespace UI.Windows
{
	[Serializable]
	[Constants("UI")]
	[Documentation("https://www.notion.so/winzardy/Window-8f15112b45b24c43b35f72f40dd4771d?pvs=4")]
	public struct UIWindowConfig
	{
		[Tooltip("Определяет порядок перекрытия окон. Окна с меньшим приоритетом не открываются поверх текущего и попадают в очередь")]
		public int priority;
		public WidgetFlags flags;
		public UILayoutEntry<UIBaseWindowLayout> layout;
	}
}
