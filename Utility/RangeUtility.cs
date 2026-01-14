using Sapientia;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class RangeUtility
	{
		public static bool Contains(this in Range<float> range, Vector3 vector, bool maxInclusive = false)
			=> vector.sqrMagnitude >= range.min * range.min &&
				(maxInclusive
					? vector.sqrMagnitude < range.max * range.max
					: vector.sqrMagnitude <= range.max * range.max);
	}
}
