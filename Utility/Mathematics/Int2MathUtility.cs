using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Burst;
using Unity.Mathematics;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9fbfde07890f4fe6b17d69cb6c0462e5
	/// </summary>
	public static class Int2MathUtility
	{
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 DivRem(this in int2 v, int2 divider, out int2 remainder)
		{
			remainder = v % divider;
			return v / divider;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Clamp(this in int2 v, int2 min, int2 max)
		{
			return new int2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Clamp(this in int2 v, int min, int max)
		{
			return new int2(v.x.Clamp(min, max), v.y.Clamp(min, max));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Mul(this in int2 v)
		{
			return v.x * v.y;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Rotate_90(this in int2 v)
		{
			return new int2(-v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Rotate_90(this in int2 v, int count)
		{
			switch (count)
			{
				case 0:
					return v;
				case 1:
					return new int2(-v.y, v.x);
				case 2:
					return new int2(-v.x, -v.y);
				case 3:
					return new int2(v.y, -v.x);
				default:
					return Rotate_90(v, count % 4);
			}
		}
	}
}
