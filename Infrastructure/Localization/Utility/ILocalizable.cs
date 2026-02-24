using System.Collections.Generic;
using Sapientia.Collections;

namespace Localization
{
	public interface ILocalizable
	{
		void ApplyLocalization();
	}

	public static class LocalizableExtensions
	{
		public static void TryApplyLocalization<T>(this IEnumerable<T> enumerable)
		{
			if (enumerable.IsNullOrEmpty())
				return;

			foreach (var obj in enumerable)
			{
				if (obj is ILocalizable localizable)
					localizable.ApplyLocalization();
			}
		}

		public static void ApplyLocalization<T>(this IEnumerable<T> enumerable)
			where T : ILocalizable
		{
			if (enumerable.IsNullOrEmpty())
				return;

			foreach (var localizable in enumerable)
			{
				localizable.ApplyLocalization();
			}
		}
	}
}
