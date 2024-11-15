using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusumity.Attributes.Odin;
using Fusumity.Attributes.Specific;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusumity.Collections
{
	[Serializable]
	public class ReorderableEnumArray<TEnum> : ReorderableEnumArray<TEnum, EmptyStruct> where TEnum : unmanaged, Enum {}

	[Serializable]
	public class ReorderableEnumArray<TEnum, TValue> : ReorderableEnumArray<TEnum, TValue, EnumValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum {}

	[Serializable]
	public class ReorderableEnumReferenceArray<TEnum, TValue> : ReorderableEnumArray<TEnum, TValue, EnumReferenceValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum
		where TValue : class {}

	[Serializable]
	public class ReorderableEnumArray<TEnum, TValue, TEnumValue> : EnumArray<TEnum, TValue, TEnumValue>
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>, new()
	{
		protected override bool IsReorderable => true;

		[SerializeField, HideInInspector]
		private int[] _indexes;

		public unsafe ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe int GetIndexOf(TEnum enumValue)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue);
		}

#if UNITY_EDITOR
		public ReorderableEnumArray()
		{
			LazyInitializeInternal();
		}

		protected override void LazyInitialize()
		{
			base.LazyInitialize();
			LazyInitializeInternal();
		}

		private void LazyInitializeInternal()
		{
			if (_indexes != null)
				return;

			_indexes = new int[values.Length];
		}

		protected override void OnValuesUpdated()
		{
			if (_indexes.Length != values.Length)
				_indexes = new int[values.Length];

			FillIndexes();
		}

		private void FillIndexes()
		{
			for (var i = 0; i < values.Length; i++)
			{
				var enumValue = values[i].EnumValue;
				_indexes[GetIndexOf(enumValue)] = i;
			}
		}
#endif
	}

	[Serializable]
	public class EnumArray<TEnum> : EnumArray<TEnum, EmptyStruct> where TEnum : unmanaged, Enum {}

	[Serializable]
	public class EnumArray<TEnum, TValue> : OrderedEnumArray<TEnum, TValue, EnumValue<TEnum, TValue>>
		where TEnum : unmanaged, Enum {}

	[Serializable]
	public class EnumReferenceArray<TEnum, TValue> : OrderedEnumArray<TEnum, TValue, EnumReferenceValue<TEnum, TValue>>
		where TValue : class
		where TEnum : unmanaged, Enum {}

	public class OrderedEnumArray<TEnum, TValue, TEnumValue> : EnumArray<TEnum, TValue, TEnumValue>
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>, new()
	{
		public ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetIndexOf(TEnum enumValue)
		{
			return EnumToIndex<TEnum>.GetIndex(enumValue);
		}
	}

	[Serializable]
	public class EnumArray<TEnum, TValue, TEnumValue> :
#if UNITY_EDITOR
		ISerializationCallbackReceiver,
#endif
		IEnumArray
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>
#if UNITY_EDITOR
		, new()
#endif
	{
		[SerializeField]
		protected TEnumValue[] values;

		public ref TEnumValue this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[index];
		}

		public int Lenght
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => values.Length;
		}

		protected virtual bool IsReorderable => false;

#if UNITY_EDITOR
		public EnumArray()
		{
			LazyInitializeInternal();
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			UpdateValues();
			OnValuesUpdated();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			UpdateValues();
			OnValuesUpdated();
		}

		protected unsafe void UpdateValues()
		{
			LazyInitialize();
			if (values.Length == EnumValues<TEnum>.ENUM_LENGHT)
			{
				for (var i = 0; i < values.Length; i++)
				{
					var enumName = values[i].EnumValueName;
					if (!Enum.TryParse<TEnum>(enumName, out var enumValue) || !values[i].EnumValue.Equals(enumValue))
						goto update;
				}
				if (!IsReorderable)
					Array.Sort(values);
				return;
			}
			update:

			var valuesNew = new List<TEnumValue>(EnumValues<TEnum>.ENUM_LENGHT);
			var hashSet = new HashSet<TEnum>();

			var isValid = stackalloc bool[values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				var enumName = values[i].EnumValueName;
				if (Enum.TryParse<TEnum>(enumName, out var enumValue) && !hashSet.Contains(enumValue))
				{
					values[i].EnumValue = enumValue;
					hashSet.Add(values[i].EnumValue);

					isValid[i] = true;
				}
				else
				{
					isValid[i] = false;
				}
			}
			for (var i = 0; i < values.Length; i++)
			{
				if (isValid[i])
				{
					valuesNew.Add(values[i]);
				}
				else if (Enum.IsDefined(typeof(TEnum), values[i].EnumValue) && !hashSet.Contains(values[i].EnumValue))
				{
					values[i].EnumValueName = Enum.GetName(typeof(TEnum), values[i].EnumValue);

					valuesNew.Add(values[i]);
					hashSet.Add(values[i].EnumValue);
				}
			}

			foreach (TEnum enumValue in EnumValues<TEnum>.VALUES)
			{
				if (hashSet.Contains(enumValue))
					continue;
				valuesNew.Add(new TEnumValue
				{
					EnumValue = enumValue,
					EnumValueName = Enum.GetName(typeof(TEnum), enumValue),
				});
			}

			values = valuesNew.ToArray();

			if (!IsReorderable)
				Array.Sort(values);
		}

		protected virtual void OnValuesUpdated() {}

		protected virtual void LazyInitialize()
		{
			LazyInitializeInternal();
		}

		private void LazyInitializeInternal()
		{
			if (values != null)
				return;

			values = new TEnumValue[EnumValues<TEnum>.ENUM_LENGHT];

			for (var i = 0; i < EnumValues<TEnum>.ENUM_LENGHT; i++)
			{
				values[i] = new TEnumValue
				{
					EnumValue = EnumValues<TEnum>.VALUES[i],
				};
			}
		}
#endif
	}

	public interface IEnumArray {}

	[Serializable]
	public struct EnumValue<TEnum, TValue> : IComparable<EnumValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
		[SerializeField, HideInInspector]
		private string enumValueName;

		[Sirenix.OdinInspector.HideLabel, ReadOnly]
		public TEnum enumValue;
		[Sirenix.OdinInspector.HideLabel]
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
			return enumValue.CompareTo(other.enumValue);
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

		[ReadOnly, Sirenix.OdinInspector.HideLabel]
		public TEnum enumValue;
		[Sirenix.OdinInspector.HideLabel]
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
			return enumValue.CompareTo(other.enumValue);
		}
	}

	public interface IEnumValue<TEnum>
	{
		public TEnum EnumValue { get; set; }
		public string EnumValueName { get; set; }
	}

	public struct EmptyStruct{}
}
