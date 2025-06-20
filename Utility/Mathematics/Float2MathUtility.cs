using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Burst;
using Unity.Mathematics;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9fbfde07890f4fe6b17d69cb6c0462e5
	/// </summary>
	public static class Float2MathUtility
	{
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 DivRem(this in float2 v, int2 divider, out float2 remainder)
		{
			remainder = v % divider;
			return (int2)(v / divider);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 FloorToInt_Positive(this in float2 v)
		{
			return new int2(v.x.FloorToInt_Positive(), v.y.FloorToInt_Positive());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 FloorToInt(this in float2 v)
		{
			return new int2(v.x.FloorToInt(), v.y.FloorToInt());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 CeilToInt_Positive(this in float2 v)
		{
			return new int2(v.x.CeilToInt_Positive(), v.y.CeilToInt_Positive());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 CeilToInt(this in float2 v)
		{
			return new int2(v.x.CeilToInt(), v.y.CeilToInt());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 XZ(this in float2 v, float y = 0f)
		{
			return new float3(v.x, y, v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 ZX(this in float2 v, float y = 0f)
		{
			return new float3(v.y, y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 XY(this in float2 v, float z = 0f)
		{
			return new float3(v.x, v.y, z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 YX(this in float2 v, float z = 0f)
		{
			return new float3(v.y, v.x, z);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 YZ(this in float2 v, float x = 0f)
		{
			return new float3(x, v.x, v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 ZY(this in float2 v, float x = 0f)
		{
			return new float3(x, v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsApproximatelyZero(this in float2 v)
		{
			return v.SqrMagnitude().IsApproximatelyZero();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 CosSin(this float rad)
		{
			return new float2(rad.Cos(), rad.Sin());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan(this in float2 v)
		{
			return v.y.Atan2(v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ToRad3D_AxisY(this in float2 v)
		{
			return FloatMathExt.HALF_PI - v.Atan();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Clamp(this in float2 v, in float2 min, float2 max)
		{
			return new float2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Clamp(this in float2 v, in float min, float max)
		{
			return new float2(v.x.Clamp(min, max), v.y.Clamp(min, max));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Min(this in float2 a, in float2 b)
		{
			return new float2(a.x.Min(b.x), a.y.Min(b.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Min(this in float2 a, in float2 b, in float2 c, in float2 d)
		{
			return new float2(a.x.Min(b.x, c.x, d.x), a.y.Min(b.y, c.y, d.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Max(this in float2 a, in float2 b)
		{
			return new float2(a.x.Max(b.x), a.y.Max(b.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(this in float2 v)
		{
			return v.x.Max(v.y);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Max(this in float2 a, in float2 b, in float2 c, in float2 d)
		{
			return new float2(a.x.Max(b.x, c.x, d.x), a.y.Max(b.y, c.y, d.y));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Sqr(this in float2 v)
		{
			return new float2(v.x.Sqr(), v.y.Sqr());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Abs(this in float2 v)
		{
			return new float2(v.x.Abs(), v.y.Abs());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_HalfPI(this in float2 v)
		{
			return new float2(-v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_NegativeHalfPI(this in float2 v)
		{
			return new float2(v.y, -v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_Deg(this in float2 v, float deg)
		{
			return v.Rotate_Rad(FloatMathExt.DEG_TO_RAD * deg);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_Rad(this in float2 v, float rad)
		{
			var sin = rad.Sin();
			var cos = rad.Cos();
			return new float2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
		}

		[BurstCompile]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rotate_RadX(this in float2 v, float rad)
		{
			var sin = rad.Sin();
			var cos = rad.Cos();
			return v.x * cos - v.y * sin;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_90(this in float2 v)
		{
			return new float2(-v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_90(this in float2 v, int count)
		{
			switch (count)
			{
				case 0:
					return v;
				case 1:
					return new float2(-v.y, v.x);
				case 2:
					return new float2(-v.x, -v.y);
				case 3:
					return new float2(v.y, -v.x);
				default:
					return Rotate_90(v, count % 4);
			}
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate(this in float2 v, float cos, float sin)
		{
			return new float2(v.Rotate_X(cos, sin), v.Rotate_Y(cos, sin));
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rotate_X(this in float2 v, float cos, float sin)
		{
			return v.x * cos - v.y * sin;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rotate_Y(this in float2 v, float cos, float sin)
		{
			return v.x * sin + v.y * cos;
		}

		/// <summary>
		/// A vector crossed with a scalar (z-axis) will return a vector on the 2D Cartesian plane/
		/// </summary>
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Cross(this float z, in float2 v)
		{
			return new float2(-z * v.y, z * v.x);
		}

		/// <summary>
		/// A vector crossed with a scalar (z-axis) will return a vector on the 2D Cartesian plane.
		/// </summary>
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Cross(this in float2 v, float z)
		{
			return new float2(z * v.y, -z * v.x);
		}

		/// <summary>
		/// Geometrically you will receive the oriented space of the parallelogram formed by given vectors.
		/// </summary>
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cross(this in float2 a, in float2 b)
		{
			return a.x * b.y - a.y * b.x;
		}

		/// <summary>
		/// Use Dot for finding scaled cos of angle between a and b.
		/// Use Dot for finding squared magnitude of vector sending it as a and b.
		/// </summary>
		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(this in float2 a, in float2 b)
		{
			return a.x * b.x + a.y * b.y;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cos(this in float2 a, in float2 b)
		{
			return a.Dot(b) / (a.Magnitude() * b.Magnitude());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rad(this in float2 a, in float2 b)
		{
			return a.Cos(b).Acos();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normal(this in float2 v)
		{
			return new float2(-v.y, v.x);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normalized(this in float2 v)
		{
			return v.Normalized(v.Magnitude());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normalized(this in float2 v, float vMagnitude)
		{
			return v / vMagnitude;
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(this in float2 a, in float2 b)
		{
			var delta = a - b;
			return delta.Magnitude();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrDistance(this in float2 a, in float2 b)
		{
			var delta = a - b;
			return delta.SqrMagnitude();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Magnitude(this in float2 v)
		{
			return v.SqrMagnitude().Sqrt();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrMagnitude(this in float2 v)
		{
			return v.Dot(v);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 MoveTowards(this in float2 from, in float2 to, float step)
		{
			var delta = to - from;
			var sqrMagnitude = delta.SqrMagnitude();
			if (sqrMagnitude == 0f || (step >= 0f && step * step >= sqrMagnitude))
				return to;

			var magnitude = sqrMagnitude.Sqrt();
			return from + delta * (step / magnitude);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 MoveTowardsFromZero(this float2 delta, float step)
		{
			if (delta.Equals(float2.zero))
				return delta;

			var sqrMagnitude = delta.SqrMagnitude();
			if (step >= 0f && step * step >= sqrMagnitude)
				return delta;

			var magnitude = sqrMagnitude.Sqrt();
			return delta * (step / magnitude);
		}
	}
}
