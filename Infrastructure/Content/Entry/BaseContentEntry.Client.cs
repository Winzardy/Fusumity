using System;
using Sapientia.Extensions.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace Content
{
	using UnityObject = UnityEngine.Object;

	public partial class BaseContentEntry<T>
	{
		// ReSharper disable once InconsistentNaming
		[SerializeField, FormerlySerializedAs("_value")]
		protected T value;

		public ref readonly T Value => ref ContentEditValue;
		ref T IContentEntry<T>.EditValue => ref ContentEditValue;

		protected ref T ContentEditValue
		{
			get
			{
				if (ValueType.IsSerializeReference())
				{
					_serializeReference ??= new SerializeReference<T>();
					return ref _serializeReference.Value;
				}

				return ref value;
			}
		}

		// TODO: надо очищать YAML от Null значений, потому что тупо место занимает rid: -2...
		// ReSharper disable once InconsistentNaming
		[SerializeField, SerializeReference, ClientOnly]
		private ISerializeReference<T> _serializeReference = null;

#if UNITY_EDITOR
		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!typeof(T).IsSerializeReference())
				return;

			_serializeReference ??= new SerializeReference<T>();
		}
#endif
	}

	public partial struct ContentEntry<T, TFilter>
	{
		public ref readonly T Value => ref entry.Value;
	}

	public static class BaseContentEntryExtensions
	{
		public static bool IsSerializeReference(this Type type)
		{
			if (type == null)
				return false;

			type = type.GetCollectionElementType();
			return type.IsPolymorphic() && !type.IsSubclassOf(typeof(UnityObject));
		}
	}

	public partial interface IContentEntry<T>
	{
		/// <summary>
		/// Изменение контента доступно только вне runtime!
		/// </summary>
		internal ref T EditValue { get; }
	}

	public interface IContentSerializeReference
	{
	}

	internal interface ISerializeReference<T> : IContentSerializeReference
	{
		internal ref T Value { get; }
	}

	/// <summary>
	/// Использую такой контейнер, в обход использования <see cref="object"/>.
	/// У <see cref="object"/> проблемы с массивами...
	/// </summary>
	internal class SerializeReference<T> : ISerializeReference<T>
	{
		[FormerlySerializedAs("_value")]
		[SerializeReference, ClientOnly]
		public T value;

		ref T ISerializeReference<T>.Value => ref value;
	}
}
