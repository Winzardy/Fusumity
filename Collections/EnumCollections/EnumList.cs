using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia.Extensions;
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
		List<TEnumValue>,
#if UNITY_EDITOR
		ISerializationCallbackReceiver
#endif
		where TEnum : unmanaged, Enum
		where TEnumValue : struct, IEnumValue<TEnum>
	{
		public TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => this[GetIndexOf(enumValue)];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => this[GetIndexOf(enumValue)] = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int GetIndexOf(TEnum enumValue)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue);
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
