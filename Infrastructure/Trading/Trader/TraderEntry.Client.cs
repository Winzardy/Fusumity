using Content;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Trading
{
	public partial struct TraderConfig
	{
		//public LocKey name;
		[PropertySpace(0,10)]
		[ClientOnly]
		public Sprite icon;
	}
}
