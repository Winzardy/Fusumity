using System;
using System.Runtime.CompilerServices;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fusumity.Collections
{
	[Serializable]
	public class EnumList<TEnum, TValue> : EnumList<TEnum, TValue, EnumValueEditableEnum<TEnum, TValue>>
		where TEnum : unmanaged, Enum
	{
	}

	[Serializable]
	public class EnumList<TEnum, TValue, TEnumValue> :
		IArrayContainer,
#if UNITY_EDITOR
		ISerializationCallbackReceiver
#endif
		where TEnum : unmanaged, Enum
		where TEnumValue : struct, IEnumValue<TEnum>
	{
		[SerializeField, HideLabel, InlineProperty]
		protected TEnumValue[] elements = new TEnumValue[0];

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => elements.Length;
		}

		public ref TEnumValue this[int i]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref elements[i];
		}

		public void Add(TEnumValue value)
		{
			ArrayExt.Add(ref elements, value);
		}

		public virtual void OnBeforeSerialize()
		{
#if UNITY_EDITOR
			OnValuesUpdated();
#endif
		}

		public virtual void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			OnValuesUpdated();
#endif
		}

#if UNITY_EDITOR
		protected virtual void OnValuesUpdated() {}
#endif
	}
}
