using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Unity.Mathematics;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#9fbfde07890f4fe6b17d69cb6c0462e5
	/// </summary>
	public static class Float2MathUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 DivRem(this float2 v, int2 divider, out float2 remainder)
		{
			remainder = v % divider;
			return (int2)(v / divider);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 RoundToInt(this float2 v)
		{
			return new int2(v.x.RoundToInt(), v.y.RoundToInt());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 FloorToInt_Positive(this float2 v)
		{
			return new int2(v.x.FloorToInt_Positive(), v.y.FloorToInt_Positive());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 FloorToInt(this float2 v)
		{
			return new int2(v.x.FloorToInt(), v.y.FloorToInt());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 CeilToInt_Positive(this float2 v)
		{
			return new int2(v.x.CeilToInt_Positive(), v.y.CeilToInt_Positive());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int2 CeilToInt(this float2 v)
		{
			return new int2(v.x.CeilToInt(), v.y.CeilToInt());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 XZ(this float2 v, float y = 0f)
		{
			return new float3(v.x, y, v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 ZX(this float2 v, float y = 0f)
		{
			return new float3(v.y, y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 XY(this float2 v, float z = 0f)
		{
			return new float3(v.x, v.y, z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 YX(this float2 v, float z = 0f)
		{
			return new float3(v.y, v.x, z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 YZ(this float2 v, float x = 0f)
		{
			return new float3(x, v.x, v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float3 ZY(this float2 v, float x = 0f)
		{
			return new float3(x, v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsApproximatelyZero(this float2 v)
		{
			return v.SqrMagnitude().IsApproximatelyZero();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 CosSin(this float rad)
		{
			var sin = rad.Sin();
			var cos = rad.Cos();
			return new float2(cos, sin);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan(this float2 v)
		{
			return v.y.Atan2(v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ToRad3DAxisY(this float2 v)
		{
			return FloatMathExt.HALF_PI - v.Atan();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Clamp(this float2 v, float2 min, float2 max)
		{
			return new float2(v.x.Clamp(min.x, max.x), v.y.Clamp(min.y, max.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Clamp(this float2 v, float min, float max)
		{
			return new float2(v.x.Clamp(min, max), v.y.Clamp(min, max));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Mul(this float2 v)
		{
			return v.x * v.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Min(this float2 a, float2 b)
		{
			return new float2(a.x.Min(b.x), a.y.Min(b.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Min(this float2 a, float2 b, float2 c, float2 d)
		{
			return new float2(a.x.Min(b.x, c.x, d.x), a.y.Min(b.y, c.y, d.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Max(this float2 a, float2 b)
		{
			return new float2(a.x.Max(b.x), a.y.Max(b.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(this float2 v)
		{
			return v.x.Max(v.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Max(this float2 a, float2 b, float2 c, float2 d)
		{
			return new float2(a.x.Max(b.x, c.x, d.x), a.y.Max(b.y, c.y, d.y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Sqr(this float2 v)
		{
			return new float2(v.x.Sqr(), v.y.Sqr());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Abs(this float2 v)
		{
			return new float2(v.x.Abs(), v.y.Abs());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 RotateHalfPI(this float2 v)
		{
			return new float2(-v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 RotateNegativeHalfPI(this float2 v)
		{
			return new float2(v.y, -v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 RotateDeg(this float2 v, float deg)
		{
			return v.Rotate_Rad(FloatMathExt.DEG_TO_RAD * deg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_Rad(this float2 v, float rad)
		{
			rad.SinCos(out var sin, out var cos);
			return new float2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate_90(this float2 v, int count)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Rotate(this float2 v, float cos, float sin)
		{
			return new float2(v.Rotate_X(cos, sin), v.Rotate_Y(cos, sin));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rotate_X(this float2 v, float cos, float sin)
		{
			return v.x * cos - v.y * sin;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rotate_Y(this float2 v, float cos, float sin)
		{
			return v.x * sin + v.y * cos;
		}

		/// <summary>
		/// A vector crossed with a scalar (z-axis) will return a vector on the 2D Cartesian plane/
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Cross(this float z, float2 v)
		{
			return new float2(-z * v.y, z * v.x);
		}

		/// <summary>
		/// A vector crossed with a scalar (z-axis) will return a vector on the 2D Cartesian plane.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Cross(this float2 v, float z)
		{
			return new float2(z * v.y, -z * v.x);
		}

		/// <summary>
		/// Geometrically you will receive the oriented space of the parallelogram formed by given vectors.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cross(this float2 a, float2 b)
		{
			return a.x * b.y - a.y * b.x;
		}

		/// <summary>
		/// Use Dot for finding scaled cos of angle between a and b (Scaled means `dot = cos(a, b) * a.magnitude * b.magnitude`).
		/// Use Dot for finding squared magnitude of vector sending it as a and b.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(this float2 a, float2 b)
		{
			return a.x * b.x + a.y * b.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cos(this float2 a, float2 b)
		{
			return a.Dot(b) / (a.Magnitude() * b.Magnitude());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Rad(this float2 a, float2 b)
		{
			return a.Cos(b).Acos();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normal(this float2 v)
		{
			return new float2(-v.y, v.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normalized(this float2 v)
		{
			return v.Normalized(v.Magnitude());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 Normalized(this float2 v, float vMagnitude)
		{
			return v / vMagnitude;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(this float2 a, float2 b)
		{
			var delta = a - b;
			return delta.Magnitude();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrDistance(this float2 a, float2 b)
		{
			var delta = a - b;
			return delta.SqrMagnitude();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Magnitude(this float2 v)
		{
			return v.SqrMagnitude().Sqrt();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SqrMagnitude(this float2 v)
		{
			return v.Dot(v);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float2 MoveTowards(this float2 from, float2 to, float step)
		{
			var delta = to - from;
			var sqrMagnitude = delta.SqrMagnitude();
			if (sqrMagnitude == 0f || (step >= 0f && step * step >= sqrMagnitude))
				return to;

			var magnitude = sqrMagnitude.Sqrt();
			return from + delta * (step / magnitude);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
