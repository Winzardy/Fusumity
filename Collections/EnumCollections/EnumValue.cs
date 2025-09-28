using System;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public struct EnumValueEditableEnum<TEnum, TValue> : IComparable<EnumValueEditableEnum<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[HideLabel]
		public TEnum enumValue;
		[HideLabel]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumValueEditableEnum<TEnum, TValue> other)
		{
			return enumValue.CompareTo(other.enumValue);
		}

		public static implicit operator (TEnum, TValue)(EnumValueEditableEnum<TEnum, TValue> value)
		{
			return (value.enumValue, value.value);
		}

		public static implicit operator EnumValueEditableEnum<TEnum, TValue>(EnumValue<TEnum, TValue> value)
		{
			return new EnumValueEditableEnum<TEnum, TValue>
			{
				enumValue = value.enumValue,
				value = value.value,
			};
		}
	}

	[Serializable]
	public struct EnumValueEditableEnum<TEnum> : IComparable<EnumValueEditableEnum<TEnum>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[HideLabel]
		public TEnum enumValue;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumValueEditableEnum<TEnum> other)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue).CompareTo(EnumToIndex<TEnum>.GetIndex(other.enumValue));
		}

		public static implicit operator TEnum(EnumValueEditableEnum<TEnum> value)
		{
			return value.enumValue;
		}

		public static implicit operator EnumValueEditableEnum<TEnum>(TEnum value)
		{
			return new EnumValueEditableEnum<TEnum>()
			{
				enumValue = value,
			};
		}
	}

	[Serializable]
	public struct EnumValue<TEnum> : IComparable<EnumValue<TEnum>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[HideLabel, ReadOnly]
		public TEnum enumValue;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumValue<TEnum> other)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue).CompareTo(EnumToIndex<TEnum>.GetIndex(other.enumValue));
		}

		public static implicit operator TEnum(EnumValue<TEnum> value)
		{
			return value.enumValue;
		}
	}

	[Serializable]
	public struct EnumValue<TEnum, TValue> : IComparable<EnumValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[HideLabel, ReadOnly]
		public TEnum enumValue;
		[HideLabel]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumValue<TEnum, TValue> other)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue).CompareTo(EnumToIndex<TEnum>.GetIndex(other.enumValue));
		}

		public static implicit operator (TEnum, TValue)(EnumValue<TEnum, TValue> value)
		{
			return (value.enumValue, value.value);
		}
	}

	[Serializable]
	public struct EnumReferenceValue<TEnum, TValue> : IComparable<EnumReferenceValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
		where TValue : class
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[ReadOnly, HideLabel]
		public TEnum enumValue;
		[HideLabel]
		[SerializeReference]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumReferenceValue<TEnum, TValue> other)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue).CompareTo(EnumToIndex<TEnum>.GetIndex(other.enumValue));
		}
	}

	[Serializable]
	public struct EnumReferenceValueEditableEnum<TEnum, TValue> : IComparable<EnumReferenceValueEditableEnum<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
		where TValue : class
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[HideLabel]
		public TEnum enumValue;
		[HideLabel]
		[SerializeReference]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}

		public int CompareTo(EnumReferenceValueEditableEnum<TEnum, TValue> other)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue).CompareTo(EnumToIndex<TEnum>.GetIndex(other.enumValue));
		}
	}

	public interface IEnumValue<TEnum>
	{
		public TEnum EnumValue { get; set; }
		public string EnumValueName { get; set; }
	}

	public struct EmptyStruct{}
}
