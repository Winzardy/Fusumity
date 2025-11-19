using System;
using Content;

namespace UI.Layers
{
	[Serializable]
	[Constants("UI")]
	[Documentation("https://www.notion.so/winzardy/Layers-ac7d6403080e46efb0d1cb488b2aaac0?pvs=4")]
	public class UILayerConfig
	{
		public int sortOrder;

		public UILayerLayout template;
	}
}
