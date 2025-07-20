#if UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Content.Management
{
	public sealed partial class ContentResolver : IContentEditorResolver
	{
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.Any<T>() => Any<T>();

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.Contains<T>(in SerializableGuid guid) => Contains<T>(in guid);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.Contains<T>(string id) => Contains<T>(id);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.Contains<T>(int index) => Contains<T>(index);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.Contains<T>() => Contains<T>();

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		UniqueContentEntry<T> IContentEditorResolver.GetEntry<T>(in SerializableGuid guid) => GetEntry<T>(in guid);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		UniqueContentEntry<T> IContentEditorResolver.GetEntry<T>(string id) => GetEntry<T>(id);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		UniqueContentEntry<T> IContentEditorResolver.GetEntry<T>(int index) => GetEntry<T>(index);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		SingleContentEntry<T> IContentEditorResolver.GetEntry<T>() => GetEntry<T>();

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.TryGetEntry<T>(in SerializableGuid guid, out UniqueContentEntry<T> entry)
			=> TryGetEntry(in guid, out entry);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.TryGetEntry<T>(string id, out UniqueContentEntry<T> entry)
			=> TryGetEntry(id, out entry);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.TryGetEntry<T>(int index, out UniqueContentEntry<T> entry)
			=> TryGetEntry(index, out entry);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IContentEditorResolver.TryGetEntry<T>(out SingleContentEntry<T> entry)
			=> TryGetEntry(out entry);
		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly T IContentEditorResolver.Get<T>(in SerializableGuid guid) => ref Get<T>(in guid);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly T IContentEditorResolver.Get<T>(string id) => ref Get<T>(id);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly T IContentEditorResolver.Get<T>(int index) => ref Get<T>(index);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly T IContentEditorResolver.Get<T>() => ref Get<T>();

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerable<IContentEntry<T>> IContentEditorResolver.GetAll<T>() => GetAll<T>();

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		string IContentEditorResolver.ToId<T>(in SerializableGuid guid) => ToId<T>(in guid);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		string IContentEditorResolver.ToId<T>(int index) => ToId<T>(index);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly SerializableGuid IContentEditorResolver.ToGuid<T>(string id) => ref ToGuid<T>(id);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ref readonly SerializableGuid IContentEditorResolver.ToGuid<T>(int index) => ref ToGuid<T>(index);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IContentEditorResolver.ToIndex<T>(in SerializableGuid guid) => ToIndex<T>(in guid);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IContentEditorResolver.ToIndex<T>(string id) => ToIndex<T>(id);

		/// <inheritdoc/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		string IContentEditorResolver.ToLabel<T>(in SerializableGuid guid, bool verbose) => ToLabel<T>(in guid, verbose);
	}

	public interface IContentEditorResolver
	{
		/// <inheritdoc cref="ContentResolver.Any{T}()"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Any<T>();

		/// <inheritdoc cref="ContentResolver.Contains{T}(in Content.SerializableGuid)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains<T>(in SerializableGuid guid);

		/// <inheritdoc cref="ContentResolver.Contains{T}"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains<T>(string id);

		/// <inheritdoc cref="Has"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains<T>(int index);

		/// <inheritdoc cref="ContentResolver.Contains{T}()"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains<T>();

		/// <inheritdoc cref="ContentResolver.GetEntry{T}(in SerializableGuid)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UniqueContentEntry<T> GetEntry<T>(in SerializableGuid guid);

		/// <inheritdoc cref="ContentResolver.GetEntry{T}(string)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UniqueContentEntry<T> GetEntry<T>(string id);

		/// <inheritdoc cref="ContentResolver.GetEntry{T}(int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UniqueContentEntry<T> GetEntry<T>(int index);

		/// <inheritdoc cref="ContentResolver.GetEntry{T}()"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SingleContentEntry<T> GetEntry<T>();

		/// <inheritdoc cref="ContentResolver.TryGetEntry{T}(in SerializableGuid, out UniqueContentEntry{T})"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetEntry<T>(in SerializableGuid guid, out UniqueContentEntry<T> entry);

		/// <inheritdoc cref="ContentResolver.TryGetEntry{T}(string, out UniqueContentEntry{T})"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetEntry<T>(string id, out UniqueContentEntry<T> entry);

		/// <inheritdoc cref="ContentResolver.TryGetEntry{T}(int, out UniqueContentEntry{T})"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetEntry<T>(int index, out UniqueContentEntry<T> entry);

		/// <inheritdoc cref="ContentResolver.TryGetEntry{T}(out SingleContentEntry{T})"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetEntry<T>(out SingleContentEntry<T> entry);

		/// <inheritdoc cref="ContentResolver.Get{T}(in SerializableGuid)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly T Get<T>(in SerializableGuid guid);

		/// <inheritdoc cref="ContentResolver.Get{T}(string)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly T Get<T>(string id);

		/// <inheritdoc cref="ContentResolver.Get{T}(int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly T Get<T>(int index);

		/// <inheritdoc cref="ContentResolver.Get{T}()"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly T Get<T>();

		/// <inheritdoc cref="ContentResolver.GetAll{T}()"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IEnumerable<IContentEntry<T>> GetAll<T>();

		/// <inheritdoc cref="ContentResolver.ToId{T}(in SerializableGuid)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToId<T>(in SerializableGuid guid);

		/// <inheritdoc cref="ContentResolver.ToId{T}(int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToId<T>(int index);

		/// <inheritdoc cref="ContentResolver.ToGuid{T}(string)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly SerializableGuid ToGuid<T>(string id);

		/// <inheritdoc cref="ContentResolver.ToGuid{T}(int)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref readonly SerializableGuid ToGuid<T>(int index);

		/// <inheritdoc cref="ContentResolver.ToIndex{T}(in SerializableGuid)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ToIndex<T>(in SerializableGuid guid);

		/// <inheritdoc cref="ContentResolver.ToIndex{T}(string)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ToIndex<T>(string id);

		/// <inheritdoc cref="ContentResolver.ToLabel{T}(in SerializableGuid, bool)"/>
		public string ToLabel<T>(in SerializableGuid guid, bool verbose = false);
	}
}
#endif
