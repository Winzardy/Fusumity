using System;
using System.Runtime.CompilerServices;
using Sapientia.Collections;
using Sapientia.Extensions;

namespace Fusumity.Collections
{
	[Serializable]
	public class EnumDictionary<TEnum, TValue> : BaseEnumList<TEnum, EnumValueEditableEnum<TEnum, TValue>>
		where TEnum : unmanaged, Enum
	{
		private int[] _enumIndexToIndex;

		public ref EnumValueEditableEnum<TEnum, TValue> this[TEnum enumValue]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref elements[GetIndexOf(enumValue)];
		}

		public bool TryGetValue(TEnum enumValue, out TValue value)
		{
			return TryGetValue(enumValue, out value, out _);
		}

		public bool TryGetValue(TEnum enumValue, out TValue value, out int index)
		{
			index = GetIndexOf(enumValue);
			if (index >= 0)
			{
				value = elements[index].value;
				return true;
			}

			value = default;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int GetIndexOf(TEnum enumValue)
		{
			var enumIndex = EnumToIndex<TEnum>.GetIndex(enumValue);
			return _enumIndexToIndex[enumIndex];
		}

		public override void OnAfterDeserialize()
		{
			_enumIndexToIndex = new int[EnumValues<TEnum>.ENUM_LENGHT];
			_enumIndexToIndex.Fill(-1);
			for (var i = 0; i < elements.Length; i++)
			{
				var enumIndex = EnumToIndex<TEnum>.GetIndex(elements[i].enumValue);
				_enumIndexToIndex[enumIndex] = i;
			}

			base.OnAfterDeserialize();
		}
	}
}
