using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Extensions;
using Unity.Collections.LowLevel.Unsafe;

namespace SharedLogic
{
	public class CommandBuffer : IDisposable
	{
		private Dictionary<Type, ICommandBuffer> _typeToBuffer;

		private Queue<Entry> _queue;

		public int Count => _queue.Count;

		public CommandBuffer()
		{
			_typeToBuffer = new();
			_queue = new();
		}

		public void Dispose()
		{
			_typeToBuffer.Clear();
			_typeToBuffer = null;

			_queue.Clear();
			_queue = null;
		}

		public void Clear()
		{
			foreach (var buffer in _typeToBuffer.Values)
				buffer.Clear();
		}

		public void Enqueue<T>(in T command)
			where T : struct, ICommand
		{
			var type = typeof(T);
			if (!_typeToBuffer.TryGetValue(type, out var buffer))
				_typeToBuffer[type] = buffer = new CommandBuffer<T>();

			var index = buffer.Add(in command);
			_queue.Enqueue(new Entry
			{
				type = type,
				index = index
			});
		}

		public void Dequeue()
		{
			_queue.Dequeue();
		}

		public void Execute(ISharedRoot root)
		{
			var entry = _queue.Peek();
			_typeToBuffer[entry.type].Execute(root, entry.index);
		}

		public bool Validate(ISharedRoot root, out Exception exception)
		{
			var entry = _queue.Peek();
			return _typeToBuffer[entry.type]
			   .Validate(root, entry.index, out exception);
		}

		public void Send(ICommandSender sender)
		{
			var entry = _queue.Peek();
			_typeToBuffer[entry.type]
			   .Send(sender, entry.index);
		}

		private struct Entry
		{
			public Type type;
			public int index;
		}
	}

	internal class CommandBuffer<T> : ICommandBuffer
		where T : struct, ICommand
	{
		private readonly SimpleList<T> _buffer = new();

		public int Add<T1>(in T1 command)
			where T1 : struct, ICommand
		{
			ref var r = ref UnsafeExt.AsRef(in command);
			ref var c = ref UnsafeExt.As<T1, T>(ref r);
			return Add(in c);
		}

		private int Add(in T command)
		{
			return _buffer.Add(in command);
		}

		public void Clear() => _buffer.Clear();

		public void Execute(ISharedRoot root, int index)
		{
			_buffer[index].Execute(root);
		}

		public bool Validate(ISharedRoot root, int index, out Exception exception)
		{
			return _buffer[index].Validate(root, out exception);
		}

		public void Send(ICommandSender sender, int index)
		{
			ref var command = ref _buffer[index];
			sender.Send(in command);
		}
	}

	internal interface ICommandBuffer
	{
		public int Add<T>(in T command)
			where T : struct, ICommand;

		public void Clear();

		public void Execute(ISharedRoot root, int index);
		public bool Validate(ISharedRoot root, int index, out Exception exception);
		public void Send(ICommandSender sender, int index);
	}
}
