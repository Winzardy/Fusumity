using System;
using System.IO;
using System.Runtime.CompilerServices;
using Sapientia;
using UnityEngine;

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

		public static bool ExecuteCommand<T>(in T command)
			where T : struct, ICommand
		{
			try
			{
				return router.ExecuteCommand(in command);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[SharedLogicManager.ExecuteCommand] ок на старте приложение, пока не убрали инициализацию из бут тасок: {ex.Message}"); //PTODO
				return false;
			}
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

		public static bool ExecuteCommand<T>(this ISharedRoot _, in T command)
			where T : struct, ICommand
			=> SharedLogicManager.ExecuteCommand(in command);
	}
}
