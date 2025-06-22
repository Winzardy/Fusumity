using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Mathematics;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9fbfde07890f4fe6b17d69cb6c0462e5
	/// </summary>
	public static class Int2MathUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Abs(this int2 v)
		{
			return new int2(v.x.Abs(), v.y.Abs());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Sign(this int2 v)
		{
			return new int2(v.x.Sign(), v.y.Sign());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 DivRem(this int2 v, int2 divider, out int2 remainder)
		{
			remainder = v % divider;
			return v / divider;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Mul(this int2 v)
		{
			return v.x * v.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Min(this int2 a, int2 b)
		{
			return new int2(a.x.Min(b.x), a.y.Min(b.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Max(this int2 v)
		{
			return v.x.Max(v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Max(this int2 a, int2 b)
		{
			return new int2(a.x.Max(b.x), a.y.Max(b.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Clamp(this int2 v, int2 min, int2 max)
		{
			return new int2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Clamp(this int2 v, int min, int max)
		{
			return new int2(v.x.Clamp(min, max), v.y.Clamp(min, max));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Rotate_90(this int2 v)
		{
			return new int2(-v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 Rotate_90(this int2 v, int count)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan(this int2 v)
		{
			return ((float2)v).Atan();
		}
	}
}
