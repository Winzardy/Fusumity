using System;
using Sapientia;

namespace Fusumity.Utility
{
	using UnityRandom = UnityEngine.Random;

	public static class UnityRandomizer<T>
		where T : struct, IComparable<T>
	{
		private static IRandomizer<T> _cache;

		public static IRandomizer<T> Default => _cache ??= GetByType();

		private static IRandomizer<T> GetByType()
		{
			var type = typeof(T);
			if (type == typeof(int))
				return (IRandomizer<T>) new UnityRandomizerInt32();
			if (type == typeof(float))
				return (IRandomizer<T>) new UnityRandomizerFloat();

			throw new NotImplementedException("Not implemented randomizer for type: " + type);
		}
	}

	public class UnityRandomizerInt32 : IRandomizer<int>
	{
		public int Next() => UnityRandom.Range(int.MinValue, int.MaxValue);
		public int Next(int maxExclusive) => UnityRandom.Range(0, maxExclusive);
		public int Next(int minInclusive, int maxExclusive) => UnityRandom.Range(minInclusive, maxExclusive);
	}

	public class UnityRandomizerFloat : IRandomizer<float>
	{
		public float Next() => UnityRandom.Range(float.MinValue, float.MaxValue);
		public float Next(float maxExclusive) => UnityRandom.Range(0, maxExclusive);
		public float Next(float minInclusive, float maxExclusive) => UnityRandom.Range(minInclusive, maxExclusive);
	}
}
