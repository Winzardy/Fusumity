using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusumity.Attributes.Specific;
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
		where TValue : class
		where TEnum : unmanaged, Enum {}

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
			return _indexes[*(int*)(&enumValue)];
		}

		protected override void LazyInitialize()
		{
			base.LazyInitialize();
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

		private unsafe void FillIndexes()
		{
			for (var i = 0; i < values.Length; i++)
			{
				var enumValue = values[i].EnumValue;
				_indexes[*(int*)(&enumValue)] = i;
			}
		}
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
		public unsafe ref TEnumValue this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref values[GetIndexOf(enumValue)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe int GetIndexOf(TEnum enumValue)
		{
			return *(int*)(&enumValue);
		}
	}

	[Serializable]
	public class EnumArray<TEnum, TValue, TEnumValue> : ISerializationCallbackReceiver, IEnumArray
		where TEnum : unmanaged, Enum
		where TEnumValue : IEnumValue<TEnum>, new()
	{
		private static readonly Array ENUM_VALUES = Enum.GetValues(typeof(TEnum));

		[SerializeField, HideLabel]
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

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			UpdateValues();
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			UpdateValues();
		}

		public void UpdateValues()
		{
			LazyInitialize();

			if (values.Length == ENUM_VALUES.Length)
			{
#if UNITY_EDITOR
				for (var i = 0; i < values.Length; i++)
				{
					var enumName = values[i].EnumValueName;
					if (!Enum.TryParse<TEnum>(enumName, out var enumValue) || !values[i].EnumValue.Equals(enumValue))
						goto update;
				}
#endif
				if (!IsReorderable)
					Array.Sort(values);
				return;
			}
			update:

			var valuesNew = new List<TEnumValue>(ENUM_VALUES.Length);
			var hashSet = new HashSet<TEnum>(values.Length);

#if UNITY_EDITOR
			Span<bool> isValid = stackalloc bool[values.Length];

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
#else
			for (var i = 0; i < values.Length; i++)
			{
				valuesNew.Add(values[i]);
				hashSet.Add(values[i].EnumValue);
			}
#endif

			foreach (TEnum enumValue in ENUM_VALUES)
			{
				if (hashSet.Contains(enumValue))
					continue;
				valuesNew.Add(new TEnumValue
				{
					EnumValue = enumValue,
#if UNITY_EDITOR
					EnumValueName = Enum.GetName(typeof(TEnum), enumValue),
#endif
				});
			}

			values = valuesNew.ToArray();

			if (!IsReorderable)
				Array.Sort(values);
		}

		protected virtual void OnValuesUpdated() {}

		protected virtual void LazyInitialize()
		{
			if (values != null)
				return;

			values = new TEnumValue[ENUM_VALUES.Length];

			for (var i = 0; i < ENUM_VALUES.Length; i++)
			{
				values[i] = new TEnumValue
				{
					EnumValue = (TEnum)ENUM_VALUES.GetValue(i),
				};
			}
		}
	}

	public interface IEnumArray {}

	[Serializable]
	public struct EnumValue<TEnum, TValue> : IComparable<EnumValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
	{
#if UNITY_EDITOR
		[SerializeField, HideInInspector]
		private string enumValueName;
#endif

		[Readonly, HideLabel, DrawOffset(xOffset = -15f)]
		public TEnum enumValue;
		[Label(""), DrawOffset(offsetLines = -1, foldoutIndent = 1, xOffset = -14f)]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

#if UNITY_EDITOR
		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}
#endif

		public int CompareTo(EnumValue<TEnum, TValue> other)
		{
			return enumValue.CompareTo(other.enumValue);
		}
	}

	[Serializable]
	public struct EnumReferenceValue<TEnum, TValue> : IComparable<EnumReferenceValue<TEnum, TValue>>, IEnumValue<TEnum>
		where TEnum: unmanaged, Enum
		where TValue : class
	{
#if UNITY_EDITOR
		[SerializeField, HideInInspector]
		private string enumValueName;
#endif

		[Readonly, HideLabel, DrawOffset(xOffset = -16f)]
		public TEnum enumValue;
		[HideLabel]
		[SerializeReference, ReferenceSelection]
		public TValue value;

		TEnum IEnumValue<TEnum>.EnumValue
		{
			get => enumValue;
			set => enumValue = value;
		}

#if UNITY_EDITOR
		string IEnumValue<TEnum>.EnumValueName
		{
			get => enumValueName;
			set => enumValueName = value;
		}
#endif

		public int CompareTo(EnumReferenceValue<TEnum, TValue> other)
		{
			return enumValue.CompareTo(other.enumValue);
		}
	}

	public interface IEnumValue<TEnum>
	{
		public TEnum EnumValue { get; set; }
#if UNITY_EDITOR
		public string EnumValueName { get; set; }
#endif
	}

	public struct EmptyStruct{}
}