using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class UnityRandomUtility
	{
		/// <summary>
		/// Uses UnityEngine random.
		/// </summary>
		public static T GetRandomValue<T>(this IList<T> source)
		{
			if (source.Count == 0)
				return default;
			if (source.Count == 1)
				return source[0];

			int index = UnityEngine.Random.Range(0, source.Count);
			return source[index];
		}

		public static T GetRandomValue<T>(this ICollection<T> source)
		{
			var list = source.ToList();
			return list.GetRandomValue();
		}

		/// <summary>
		/// Uses UnityEngine random.
		/// </summary>
		public static T[] GetRandomValues<T>(this IList<T> source, int amount, bool uniqueValues = true)
		{
			if (uniqueValues)
			{
				if (source.Count < amount)
				{
					throw new ArgumentOutOfRangeException("Amount of elements requested from the collection exceeds collection length.");
				}

				var copy = new List<T>(source);
				copy.Shuffle();

				var values = new T[amount];
				for (int i = 0; i < amount; i++)
				{
					values[i] = copy[i];
				}

				return values;
			}
			else
			{
				var values = new T[amount];
				for (int i = 0; i < amount; i++)
				{
					values[i] = source.GetRandomValue();
				}

				return values;
			}
		}

		/// <summary>
		/// Uses UnityEngine random.
		/// </summary>
		public static void Shuffle<T>(this IList<T> ts)
		{
			var count = ts.Count;
			var last = count - 1;
			for (var i = 0; i < last; ++i)
			{
				var r = UnityEngine.Random.Range(i, count);
				var tmp = ts[i];
				ts[i] = ts[r];
				ts[r] = tmp;
			}
		}
	}
}
