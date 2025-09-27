using Content;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SharedLogic
{
	public partial struct SharedLogicConfiguration
	{
		[ClientOnly]
		[SerializeReference]
		[Title("Client")]
		public ICommandSenderFactory commandSender;
	}
}
