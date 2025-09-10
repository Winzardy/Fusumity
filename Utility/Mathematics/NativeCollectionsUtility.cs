using System;
using System.Runtime.CompilerServices;
using Sapientia.Data;
using Sapientia.Extensions;
using Submodules.Sapientia.Memory;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Fusumity.Utility
{
	public static unsafe class NativeCollectionsUtility
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Fill<T>(this UnsafeList<T> list, in T value, int startIndex, int length) where T : unmanaged
		{
			list.Length = list.Length.Max(startIndex + length);

			var values = list.GetSafePtr();
			MemoryExt.MemFill(value, values + startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Fill<T>(this NativeArray<T> array, in T value, int startIndex, int length) where T : unmanaged
		{
			var values = array.GetSafePtr();
			MemoryExt.MemFill(value, values + startIndex, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Fill<T>(this NativeArray<T> array, in T value) where T : unmanaged
		{
			var values = array.GetSafePtr();
			MemoryExt.MemFill(value, values, array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Reverse<T>(this NativeArray<T> array) where T : unmanaged
		{
			for (int i = 0, j = array.Length - 1; i < j; i++, j--)
			{
				(array[i], array[j]) = (array[j], array[i]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Reverse<T>(this NativeList<T> array) where T : unmanaged
		{
			for (int i = 0, j = array.Length - 1; i < j; i++, j--)
			{
				(array[i], array[j]) = (array[j], array[i]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T GetValue<T>(this NativeList<T> list, int index) where T : unmanaged
		{
			return ref *(list.GetUnsafePtr() + index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> GetSpan<T>(this NativeList<T> list) where T : unmanaged
		{
			return new Span<T>(list.GetUnsafePtr(), list.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> GetSpan<T>(this UnsafeList<T> list) where T : unmanaged
		{
			return new Span<T>(list.Ptr, list.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> GetSpan<T>(this NativeArray<T> array) where T : unmanaged
		{
			return new Span<T>(array.GetUnsafePtr(), array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<T> GetSpan<T>(this NativeSlice<T> array) where T : unmanaged
		{
			return new Span<T>(array.GetUnsafePtr(), array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SafePtr<T> GetSafePtr<T>(this NativeList<T> list) where T : unmanaged
		{
			return new SafePtr<T>(list.GetUnsafePtr(), list.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SafePtr<T> GetSafePtr<T>(this UnsafeList<T> list) where T : unmanaged
		{
			return new SafePtr<T>(list.Ptr, list.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SafePtr<T> GetSafePtr<T>(this NativeArray<T> array) where T : unmanaged
		{
			return new SafePtr<T>(array.GetUnsafePtr(), array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SafePtr<T> GetSafePtr<T>(this NativeSlice<T> array) where T : unmanaged
		{
			return new SafePtr<T>(array.GetUnsafePtr(), array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T GetValue<T>(this NativeArray<T> array, int index) where T : unmanaged
		{
			return ref *((T*)array.GetUnsafePtr() + index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref T GetValue<T>(this NativeSlice<T> array, int index) where T : unmanaged
		{
			return ref *((T*)array.GetUnsafePtr() + index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear<T>(this NativeArray<T> array, int index, int count) where T : unmanaged
		{
			UnsafeUtility.MemClear((T*)array.GetUnsafePtr() + index, TSize<T>.size * count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear<T>(this NativeArray<T> array, int count) where T : unmanaged
		{
			UnsafeUtility.MemClear(array.GetUnsafePtr(), TSize<T>.size * count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear<T>(this NativeArray<T> array) where T : unmanaged
		{
			UnsafeUtility.MemClear(array.GetUnsafePtr(), TSize<T>.size * array.Length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Resize<T>(this ref NativeArray<T> array, int newSize, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : unmanaged
		{
			var newArray = new NativeArray<T>(newSize, allocator, options);
			MemoryExt.MemCopy<T>(array.GetSafePtr(), newArray.GetSafePtr(), array.Length.Min(newSize));

			array = newArray;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EnsureLength(this ref NativeBitArray bitArray, int length, NativeArrayOptions options =  NativeArrayOptions.ClearMemory)
		{
			if (bitArray.Length < length)
				bitArray.Resize(length, options);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EnsureSet(this ref NativeBitArray bitArray, int pos, bool value)
		{
			if (bitArray.Length <= pos)
				bitArray.Resize(pos + 1, NativeArrayOptions.ClearMemory);
			bitArray.Set(pos, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool EnsureIsSet(this in NativeBitArray bitArray, int pos)
		{
			if (pos < 0 || bitArray.Length <= pos)
				return false;
			return bitArray.IsSet(pos);
		}
	}
}
