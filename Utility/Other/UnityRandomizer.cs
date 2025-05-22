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
		public int Next(int max) => UnityRandom.Range(0, max);
		public int Next(int min, int max) => UnityRandom.Range(min, max);
	}

	public class UnityRandomizerFloat : IRandomizer<float>
	{
		public float Next() => UnityRandom.Range(float.MinValue, float.MaxValue);
		public float Next(float max) => UnityRandom.Range(0, max);
		public float Next(float min, float max) => UnityRandom.Range(min, max);
	}
}
