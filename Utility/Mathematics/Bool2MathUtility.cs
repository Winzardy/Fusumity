using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Generic.Extensions
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9fbfde07890f4fe6b17d69cb6c0462e5
	/// </summary>
	public static class Bool2MathUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool All(this in bool2 v)
		{
			return v is { x: true, y: true };
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Any(this in bool2 v)
		{
			return v.x || v.y;
		}
	}
}
