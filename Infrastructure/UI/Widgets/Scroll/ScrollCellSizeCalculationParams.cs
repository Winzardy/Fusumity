using System;
using Sapientia;

namespace UI.Scroll
{
	[Serializable]
	public struct ScrollCellSizeOptions
	{
		public OptionalRange<int> minMaxSize;

		public int size;
		public int spacing;

		public int paddingStart;
		public int paddingEnd;
	}

	public static class ScrollCellSizeUtility
	{
		public static int Calculate(this in ScrollCellSizeOptions info, int count)
		{
			if (count < 1)
				count = 1;

			var size = info.paddingStart +
				count * info.size +
				(count - 1) * info.spacing +
				info.paddingEnd;

			if (info.minMaxSize.max)
			{
				if (size > info.minMaxSize.max)
					size = info.minMaxSize.max;
			}

			if (info.minMaxSize.min)
			{
				if (size < info.minMaxSize.min)
					size = info.minMaxSize.min;
			}

			return size;
		}
	}
}
