using Sapientia.Extensions;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class Float2ClippingUtility
	{
		[BurstCompile]
		public static bool ClipLineToRect(in float2 in0, in float2 in1, in Rect rect, out float2 out0, out float2 out1)
		{
			var delta = in1 - in0;

			var t0 = 0f;
			var t1 = 1f;

			out0 = out1 = float2.zero;

			for (var edge = 0; edge < 4; edge++)
			{
				float p, q;

				switch (edge)
				{
					case 0:
						p = -delta.x;
						q = -(rect.xMin - in0.x);
						break;

					case 1:
						p = delta.x;
						q = rect.xMax - in0.x;
						break;

					case 2:
						p = -delta.y;
						q = -(rect.yMin - in0.y);
						break;

					default:
						p = delta.y;
						q = rect.yMax - in0.y;
						break;
				}

				var r = q / p;

				if (Mathf.Approximately(p, 0) && q < 0)
					return false;

				if (p < 0)
				{
					if (r > t1)
						return false;

					if (r > t0)
						t0 = r;
				}
				else if (p > 0)
				{
					if (r < t0)
						return false;

					if (r < t1)
						t1 = r;
				}
			}

			out0 = Mathf.Approximately(t0, 0f) ? in0 : in0 + t0 * delta;
			out1 = Mathf.Approximately(t1, 1f) ? in1 : in0 + t1 * delta;

			return true;
		}

		[BurstCompile]
		public static void ClipLineToCircle(in float2 in0, in float2 center, in float radius, out float2 out0)
		{
			var delta = center - in0;

			var distance = delta.Magnitude();

			out0 = float2.zero;

			if (distance <= radius)
			{
				out0 = in0;
				return;
			}

			out0 = center + delta.Normalized() * radius;
		}

		[BurstCompile]
		public static void ClipLineToEllipse(in float2 in0, in float2 in1, in float2 center, in float radius1, in float radius2,
			out float2 out0, out float2 out1)
		{
			var direction = in0 - in1;

			var a = (direction.x * direction.x) / (radius1 * radius1) + (direction.y * direction.y) / (radius2 * radius2);
			var b = 2 * ((in0.x - center.x) * direction.x / (radius1 * radius1) +
				(in0.y - center.y) * direction.y / (radius2 * radius2));
			var c = ((in0.x - center.x) * (in0.x - center.x)) / (radius1 * radius1) +
				((in0.y - center.y) * (in0.y - center.y)) / (radius2 * radius2) - 1;

			var d = b * b - 4 * a * c;

			out0 = out1 = float2.zero;

			if (d < 0)
				return;

			if (d.IsApproximatelyZero())
			{
				var t = -b / (2 * a);
				out0 = in0 + t * direction;

				return;
			}

			var sqrtD = Mathf.Sqrt(d);
			var t1 = (-b - sqrtD) / (2 * a);
			var t2 = (-b + sqrtD) / (2 * a);

			out0 = in0 + t1 * direction;
			out1 = in0 + t2 * direction;
		}
	}
}
