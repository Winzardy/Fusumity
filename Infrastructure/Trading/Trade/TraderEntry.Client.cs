using Sirenix.OdinInspector;
using UnityEngine;

namespace Trading
{
	public partial struct TraderEntry
	{
		public string name;
		[PropertySpace(0,10)]
		public Sprite icon;
	}
}
