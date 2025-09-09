// using System;
// using System.Collections.Generic;
// using Sapientia;
//
// namespace SharedLogic.Client
// {
// 	public class ClientCommandRunner : ICommandRunner
// 	{
// 		private readonly ISharedRoot _root;
// 		private readonly ICommandSender _commandSender;
// 		private readonly ILogger _logger;
//
// 		private readonly Queue<ICommand> _commandQueue = new();
//
// 		public ClientCommandRunner(ISharedRoot root, ICommandSender sender, ILogger logger = null)
// 		{
// 			_root = root;
// 			_commandSender = sender;
// 			_logger = logger;
// 		}
//
// 		public void Execute(ICommand command)
// 		{
// 			if (command is ITimeCommandHolder timedCommand)
// 				timedCommand.SetTimestamp(_root.GetTimestamp());
//
// 			// we validate command here and add command to execution queue.
// 			// it will be validated again later, to make sure its validity state
// 			// has not changed during previous command execution.
// 			if (ValidateCommand(command))
// 			{
// 				ProcessCommand(command);
// 			}
// 		}
//
// 		private void ProcessCommand(ICommand command)
// 		{
// 			var isFirstInQueue = _commandQueue.Count == 0;
//
// 			_commandQueue.Enqueue(command);
//
// 			if (!isFirstInQueue)
// 				return;
//
// 			while (_commandQueue.Count > 0)
// 			{
// 				var nextCommand = _commandQueue.Peek();
//
// 				if (!ValidateCommand(nextCommand))
// 				{
// 					_commandQueue.Dequeue();
// 					continue;
// 				}
//
// 				try
// 				{
// 					nextCommand.Execute(_root);
// 					_logger?.Log($"Executed command by type [ {nextCommand} ]");
//
// 					_commandSender.SendCommand(nextCommand);
// 				}
// 				catch (Exception e)
// 				{
// 					_logger?.LogException(e);
// 				}
// 				finally
// 				{
// 					_commandQueue.Dequeue();
// 				}
// 			}
// 		}
//
// 		private bool ValidateCommand(ICommand command, bool logException = true)
// 		{
// 			if (!command.Validate(_root, out var exception))
// 			{
// 				if (logException)
// 					_logger?.LogException(exception);
//
// 				return false;
// 			}
//
// 			return true;
// 		}
// 	}
// }
