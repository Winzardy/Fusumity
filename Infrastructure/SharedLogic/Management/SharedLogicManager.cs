using System;
using System.Runtime.CompilerServices;
using Sapientia;

namespace SharedLogic
{
	/// <remarks>
	/// Такое решение подразумевает что мы используем всего один инстанс SharedRoot, если вдруг понадобится использовать несколько экземпляров, то придется поменять подход
	/// </remarks>
	public class SharedLogicManager : StaticProvider<ISharedLogicRouter>
	{
		// ReSharper disable once InconsistentNaming
		internal static ISharedLogicRouter router
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instance;
		}

		internal static long Timestamp => router.DateProvider.DateTime.Ticks;

		public static bool ExecuteCommand<T>(in T command)
			where T : struct, ICommand
		{
			return router.ExecuteCommand(in command);
		}

		public static void SetupServerTime(DateTime newDateTime)
		{
			router.SetupServerTime(newDateTime);
		}
	}

	/// <remarks>
	/// Такое решение подразумевает что мы используем всего один инстанс SharedRoot, если вдруг понадобится использовать несколько экземпляров, то придется поменять подход
	/// </remarks>
	public static class SharedLogicRouterUtility
	{
		public static bool ExecuteCommand<T>(this ISharedRoot root)
			where T : struct, ICommand
		{
			var command = new T();
			return ExecuteCommand(root, in command);
		}

		public static bool ExecuteCommand<T>(this ISharedRoot root, in T command)
			where T : struct, ICommand
		{
			var timeSetCommand = new TimeSetCommand(SharedLogicManager.Timestamp);
			SharedLogicManager.ExecuteCommand(in timeSetCommand);

			if (!command.Validate(root, out var exception))
			{
				SLDebug.LogException(exception);
				return false;
			}

			SharedLogicManager.ExecuteCommand(in command);
			return true;
		}
	}
}
