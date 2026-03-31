#if CLIENT
using UnityEngine;

namespace Trading
{
	public abstract partial class TradeCost
	{
		public static readonly Color COLOR = new(R, G, B, A);
		public const float R = 0.75f;
		public const float G = 0.4f;
		public const float B = 0;
		public const float A = 1;
	}
}
#endif
