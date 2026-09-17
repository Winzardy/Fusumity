using System.Runtime.CompilerServices;
using Sapientia;

namespace SharedLogic
{
	/// <remarks>
	/// Такое решение подразумевает что мы используем всего один инстанс SharedRoot, если вдруг понадобится использовать несколько экземпляров, то придется поменять подход
	/// </remarks>
	public class SharedLogicManager : StaticWrapper<ICommandRunner>
	{
		// ReSharper disable once InconsistentNaming
		internal static ICommandRunner runner
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}
	}

	/// <remarks>
	/// ⚠️Такое решение подразумевает что мы используем всего один инстанс SharedRoot, если вдруг понадобится использовать несколько экземпляров, то придется поменять подход
	/// </remarks>
	public static class SharedLogicRouterUtility
	{
		/// <inheritdoc cref="ICommandRunner.Execute"/>
		public static bool ExecuteCommand<T>(this ISharedRoot root) where T : struct, ICommand
		{
			return ExecuteCommand(root, new T());
		}

		/// <inheritdoc cref="ICommandRunner.Execute"/>
		public static bool ExecuteCommand<T>(this ISharedRoot _, in T command) where T : struct, ICommand
		{
			return SharedLogicManager.runner.Execute(in command);
		}

		/// <inheritdoc cref="ICommandRunner.HasPending{T}(in T)"/>
		public static bool HasPendingCommand<T>(this ISharedRoot root) where T : struct, ICommand
		{
			return HasPendingCommand(root, new T());
		}

		/// <inheritdoc cref="ICommandRunner.HasPending{T}(in T)"/>
		public static bool HasPendingCommand<T>(this ISharedRoot _, in T command) where T : struct, ICommand
		{
			return SharedLogicManager.runner.HasPending(in command);
		}
	}
}
