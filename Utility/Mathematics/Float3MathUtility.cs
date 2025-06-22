using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#de48514315cf44d1ac92615e880d4241
	/// </summary>
	public static class Float3MathUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(this float3 v)
		{
			return v.x.Max(v.y.Max(v.z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 Abs(this float3 v)
		{
			return new float3(v.x.Abs(), v.y.Abs(), v.z.Abs());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XZ(this float3 v)
		{
			return new float2(v.x, v.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XY(this float3 v)
		{
			return new float2(v.x, v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YZ(this float3 v)
		{
			return new float2(v.y, v.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YX(this float3 v)
		{
			return new float2(v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZX(this float3 v)
		{
			return new float2(v.z, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZY(this float3 v)
		{
			return new float2(v.z, v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XZ(this Vector3 v)
		{
			return new float2(v.x, v.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XY(this Vector3 v)
		{
			return new float2(v.x, v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YZ(this Vector3 v)
		{
			return new float2(v.y, v.z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YX(this Vector3 v)
		{
			return new float2(v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZX(this Vector3 v)
		{
			return new float2(v.z, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZY(this Vector3 v)
		{
			return new float2(v.z, v.y);
		}
	}
}
