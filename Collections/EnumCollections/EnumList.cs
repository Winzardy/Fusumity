using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sapientia.Collections;
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
	public class EnumList<TEnum, TValue, TEnumValue>
		: ISerializationCallbackReceiver
#if UNITY_EDITOR
			, IArrayContainer
#endif
		where TEnum : unmanaged, Enum
		where TEnumValue : struct, IEnumValue<TEnum>
	{
		[SerializeField, HideLabel, InlineProperty]
		public TEnumValue[] elements = new TEnumValue[0];

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
			OnValuesUpdated(true);
#endif
		}

		public virtual void OnAfterDeserialize()
		{
#if UNITY_EDITOR
			OnValuesUpdated(false);
#endif
		}

#if UNITY_EDITOR
		protected virtual void OnValuesUpdated(bool isSerialize)
		{
			for (var i = 0; i < elements.Length; i++)
			{
				if (!isSerialize && Enum.TryParse<TEnum>(elements[i].EnumValueName, out var enumValue))
					elements[i].EnumValue = enumValue;
				else
					elements[i].EnumValueName = elements[i].EnumValue.ToString();
			}
		}
#endif
	}

	public static class EnumListExtensions
	{
		public static IEnumerable<TEnum> ToEnums<TEnum, TData>(this EnumList<TEnum, TData> enumList)
			where TEnum : unmanaged, Enum
		{
			for (int i = 0; i < enumList.Count; i++)
				yield return enumList[i].enumValue;
		}
	}
}
