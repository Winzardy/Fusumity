using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#de48514315cf44d1ac92615e880d4241
	/// </summary>
	public static class Float3MathUtility
	{
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(this in float3 v)
		{
			return v.x.Max(v.y.Max(v.z));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 Abs(this in float3 v)
		{
			return new float3(v.x.Abs(), v.y.Abs(), v.z.Abs());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XZ(this in float3 v)
		{
			return new float2(v.x, v.z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XY(this in float3 v)
		{
			return new float2(v.x, v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YZ(this in float3 v)
		{
			return new float2(v.y, v.z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YX(this in float3 v)
		{
			return new float2(v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZX(this in float3 v)
		{
			return new float2(v.z, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZY(this in float3 v)
		{
			return new float2(v.z, v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XZ(this in Vector3 v)
		{
			return new float2(v.x, v.z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 XY(this in Vector3 v)
		{
			return new float2(v.x, v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YZ(this in Vector3 v)
		{
			return new float2(v.y, v.z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 YX(this in Vector3 v)
		{
			return new float2(v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZX(this in Vector3 v)
		{
			return new float2(v.z, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 ZY(this in Vector3 v)
		{
			return new float2(v.z, v.y);
		}
	}
}
