using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Burst;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9f22738c69fe42779f3cb4e634f8608a
	/// </summary>
	public static class Vector2MathUtility
	{
		/// <summary>
		///   <para>Shorthand for writing Vector2(0.5f, 0.5f).</para>
		/// </summary>
		public static readonly Vector2 center = new (0.5f, 0.5f);
		/// <summary>
		///   <para>Shorthand for writing Vector2(-0.5f, -0.5f).</para>
		/// </summary>
		public static readonly Vector2 negativeCenter = new (-0.5f,- 0.5f);

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Clamp(this in Vector2 v, in Vector2 min, Vector2 max)
		{
			return new Vector2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Clamp(this in Vector2 v, in float min, float max)
		{
			return new Vector2(v.x.Clamp(min, max), v.y.Clamp(min, max));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Min(this in Vector2 a, in float b)
		{
			return new Vector2(a.x.Min(b), a.y.Min(b));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Min(this in Vector2 a, in Vector2 b)
		{
			return new Vector2(a.x.Min(b.x), a.y.Min(b.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Min(this in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d)
		{
			return new Vector2(a.x.Min(b.x, c.x, d.x), a.y.Min(b.y, c.y, d.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Max(this in Vector2 a, in float b)
		{
			return new Vector2(a.x.Max(b), a.y.Max(b));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Max(this in Vector2 a, in Vector2 b)
		{
			return new Vector2(a.x.Max(b.x), a.y.Max(b.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Max(this in Vector2 a, in Vector2 b, in Vector2 c, in Vector2 d)
		{
			return new Vector2(a.x.Max(b.x, c.x, d.x), a.y.Max(b.y, c.y, d.y));
		}
	}
}
