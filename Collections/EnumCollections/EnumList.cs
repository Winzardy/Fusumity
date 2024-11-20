using System;
using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class EnumList<TEnum, TValue> : EnumList<TEnum, TValue, EnumValueEditableEnum<TEnum, TValue>>
		where TEnum : unmanaged, Enum
	{
	}

	[Serializable]
	public class EnumList<TEnum, TValue, TEnumValue> :
		IEnumArray,
#if UNITY_EDITOR
		ISerializationCallbackReceiver
#endif
		where TEnum : unmanaged, Enum
		where TEnumValue : struct, IEnumValue<TEnum>
	{
		[SerializeField, HideLabel, InlineProperty]
		private TEnumValue[] _elements = new TEnumValue[0];

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _elements.Length;
		}

		public ref TEnumValue this[int i]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref _elements[i];
		}

		public ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref _elements[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int GetIndexOf(TEnum enumValue)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue);
		}

		public void Add(TEnumValue value)
		{
			ArrayExt.Add(ref _elements, value);
		}

#if UNITY_EDITOR
		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			OnValuesUpdated();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			OnValuesUpdated();
		}

		protected virtual void OnValuesUpdated() {}
#endif
	}
}
