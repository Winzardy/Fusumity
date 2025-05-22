using Sapientia.Data;
using Sapientia.Extensions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Fusumity.Utility
{
	public static unsafe class NativeCollectionsUtility
	{
		public static void Fill<T>(this NativeArray<T> array, in T value, int startIndex, int length) where T : unmanaged
		{
			var values = array.GetSafePtr();
			MemoryExt.MemFill(value, values + startIndex, length);
		}

		public static void Fill<T>(this NativeArray<T> array, in T value) where T : unmanaged
		{
			var values = array.GetSafePtr();
			MemoryExt.MemFill(value, values, array.Length);
		}

		public static void Reverse<T>(this NativeArray<T> array) where T : unmanaged
		{
			for (int i = 0, j = array.Length - 1; i < j; i++, j--)
			{
				(array[i], array[j]) = (array[j], array[i]);
			}
		}

		public static void Reverse<T>(this NativeList<T> array) where T : unmanaged
		{
			for (int i = 0, j = array.Length - 1; i < j; i++, j--)
			{
				(array[i], array[j]) = (array[j], array[i]);
			}
		}

		public static ref T GetValue<T>(this NativeList<T> list, int index) where T : unmanaged
		{
			return ref *(list.GetUnsafePtr() + index);
		}

		public static SafePtr<T> GetSafePtr<T>(this NativeList<T> list) where T : unmanaged
		{
			return new SafePtr<T>(list.GetUnsafePtr(), list.Length);
		}

		public static SafePtr<T> GetSafePtr<T>(this NativeArray<T> array) where T : unmanaged
		{
			return new SafePtr<T>(array.GetUnsafePtr(), array.Length);
		}

		public static void Clear<T>(this NativeArray<T> array, int index, int count) where T : unmanaged
		{
			MemoryExt.MemClear(array.GetSafePtr() + index, count);
		}

		public static void Clear<T>(this NativeArray<T> array, int count) where T : unmanaged
		{
			MemoryExt.MemClear(array.GetSafePtr(), count);
		}

		public static void Clear<T>(this NativeArray<T> array) where T : unmanaged
		{
			MemoryExt.MemClear(array.GetSafePtr(), array.Length);
		}

		public static void Resize<T>(this NativeArray<T> array, int newSize, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
		{
			var newArray = new NativeArray<T>(newSize, allocator, options);
			MemoryExt.MemCopy<T>(array.GetSafePtr(), newArray.GetSafePtr(), array.Length.Min(newSize));

			array = newArray;
		}

		public static void EnsureSet(this ref NativeBitArray bitArray, int pos, bool value)
		{
			if (bitArray.Length <= pos)
				bitArray.Resize(pos + 1, NativeArrayOptions.ClearMemory);
			bitArray.Set(pos, value);
		}

		public static bool EnsureIsSet(this ref NativeBitArray bitArray, int pos)
		{
			if (bitArray.Length <= pos)
				bitArray.Resize(pos + 1, NativeArrayOptions.ClearMemory);
			return bitArray.IsSet(pos);
		}
	}
}
