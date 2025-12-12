using System.Runtime.CompilerServices;
using Fusumity.Utility;
using Sapientia.Extensions;
using Unity.Burst;
using Unity.Mathematics;

namespace Fusumity.Utility
{
	public struct Rotation
	{
		public float rad;

		public Rotation(float rad)
		{
			this.rad = rad;
		}

		public Rotation(float2 direction)
		{
			this.rad = direction.Atan();
		}

		public Rotation(int2 direction)
		{
			this.rad = direction.Atan();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rotation FromQuaternion(in quaternion quaternion)
		{
			var value = quaternion.value;
			return -(2 * value.y * value.w).Atan2(1 - 2 * value.y.Sqr());
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly quaternion ToQuaternion()
		{
			return quaternion.RotateY(-rad);
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float2 ToDirection()
		{
			return rad.CosSin();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float2 CosSin()
		{
			return rad.CosSin();
		}

		[BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float2 RotateVector(float2 vector)
		{
			return vector.Rotate_Rad(rad);
		}

		public static implicit operator float(Rotation rotation)
		{
			return rotation.rad;
		}

		public static implicit operator Rotation(float rad)
		{
			return new Rotation(rad);
		}
	}
}
