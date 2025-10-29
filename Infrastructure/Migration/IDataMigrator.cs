using System;
using Fusumity.Attributes.Specific;
using UnityEngine;

namespace Submodules.Fusumity.Infrastructure.Migration
{
	/// <summary>
	/// ВНИМАНИЕ!
	/// `ISerializationCallbackReceiver` работает только для классов.
	/// Не использовать для структур!
	/// </summary>
	[Obsolete("Наследника этого интерфейса нужно мигрировать и удалить наследование от этого интерфейса. Сам интерфейс не удалять!")]
	public interface IDataMigrator : ISerializationCallbackReceiver
	{
		public bool IsMigrated { get; set; }

		// В наследниках необходимо написать следующий код:
		/*public bool IsMigrated { get => isMigrated; set => isMigrated = value; }
		[SerializeField, ReadOnly]
		private bool isMigrated;*/

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (IsMigrated)
				return;
			Migrate();
			IsMigrated = true;
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			if (IsMigrated)
				return;
			Migrate();
			IsMigrated = true;
		}

		public void Migrate();
	}
}
