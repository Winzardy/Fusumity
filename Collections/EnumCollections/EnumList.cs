using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusumity.Attributes.Odin;
using Sapientia.Collections;
using Sapientia.Data;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class EnumReferenceList<TEnum, TValue> : BaseEnumList<TEnum, EnumReferenceValueEditableEnum<TEnum, TValue>>
		where TEnum : unmanaged, Enum
		where TValue : class
	{
	}

	[Serializable]
	public class EnumList<TEnum, TValue> : BaseEnumList<TEnum, EnumValueEditableEnum<TEnum, TValue>>
		where TEnum : unmanaged, Enum
	{
	}

	[Serializable]
	public class EnumList<TEnum> : BaseEnumList<TEnum, EnumValueEditableEnum<TEnum>>
		where TEnum : unmanaged, Enum
	{
		public EnumMask<TEnum> Mask { get; private set; }

		public override void OnAfterDeserialize()
		{
			base.OnAfterDeserialize();
			Mask = GetEnumMask();
		}

		public static implicit operator EnumList<TEnum>(TEnum value)
		{
			return new EnumList<TEnum>()
			{
				values = new EnumValueEditableEnum<TEnum>[]
				{
					value
				}
			};
		}

		public static implicit operator EnumList<TEnum>(EnumMask<TEnum> mask)
		{
			var span = (Span<TEnum>)stackalloc TEnum[EnumValues<TEnum>.ENUM_LENGHT];
			var count = 0;
			foreach (var enumValue in EnumValues<TEnum>.VALUES)
			{
				if (mask.Has(enumValue))
					span[count++] = enumValue;
			}
			var result = new EnumList<TEnum>()
			{
				values = new EnumValueEditableEnum<TEnum>[count],
			};
			for (var i = 0; i < count; i++)
			{
				result.values[i] = span[i];
			}

			return result;
		}
	}

	[Serializable]
	[SingleFieldInline]
	public class BaseEnumList<TEnum, TEnumValue>
		: ISerializationCallbackReceiver
		where TEnum : unmanaged, Enum
		where TEnumValue : struct, IEnumValue<TEnum>
	{
		public TEnumValue[] values = new TEnumValue[0];

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => values.Length;
		}

		public ref TEnumValue this[int i]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[i];
		}

		public void Add(TEnumValue value)
		{
			ArrayExt.Add(ref values, value);
		}

		public EnumMask<TEnum> GetEnumMask()
		{
			var result = new EnumMask<TEnum>();
			foreach (var value in values)
			{
				result.Add(value.EnumValue);
			}
			return result;
		}

		public IEnumerable<TEnum> ToEnums()
		{
			for (var i = 0; i < Count; i++)
				yield return values[i].EnumValue;
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
			for (var i = 0; i < values.Length; i++)
			{
				if (!isSerialize && Enum.TryParse<TEnum>(values[i].EnumValueName, out var enumValue))
					values[i].EnumValue = enumValue;
				else
					values[i].EnumValueName = values[i].EnumValue.ToString();
			}
		}
#endif
	}
}
