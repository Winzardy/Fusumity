using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class UnitParentAttribute : Attribute, IAttributeConvertible
	{
		/// <summary>The unit of underlying value.</summary>
		public Units Base = Units.Unset;

		/// <summary>The unit displayed in the number field.</summary>
		public Units Display = Units.Unset;

		/// <summary>Name of the underlying unit.</summary>
		public string BaseName;

		/// <summary>Name of the unit displayed in the number field.</summary>
		public string DisplayName;

		/// <summary>
		/// If <c>true</c> the number field is drawn as read-only text.
		/// </summary>
		public bool DisplayAsString;

		/// <summary>
		/// If <c>true</c> disables the option to change display unit with the right-click context menu.
		/// </summary>
		public bool ForceDisplayUnit;

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="unit">The unit of underlying value.</param>
		public UnitParentAttribute(Units unit)
		{
			this.Base = unit;
			this.Display = unit;
		}

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="unit">The name of the underlying value.</param>
		public UnitParentAttribute(string unit)
		{
			this.BaseName = unit;
			this.DisplayName = unit;
		}

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="base">The unit of underlying value.</param>
		/// <param name="display">The unit to display the value as in the inspector.</param>
		public UnitParentAttribute(Units @base, Units display)
		{
			this.Base = @base;
			this.Display = display;
		}

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="base">The unit of underlying value.</param>
		/// <param name="display">The unit to display the value as in the inspector.</param>
		public UnitParentAttribute(Units @base, string display)
		{
			this.Base = @base;
			this.DisplayName = display;
		}

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="base">The unit of underlying value.</param>
		/// <param name="display">The unit to display the value as in the inspector.</param>
		public UnitParentAttribute(string @base, Units display)
		{
			this.BaseName = @base;
			this.Display = display;
		}

		/// <summary>Displays the number as a unit field.</summary>
		/// <param name="base">The unit of underlying value.</param>
		/// <param name="display">The unit to display the value as in the inspector.</param>
		public UnitParentAttribute(string @base, string display)
		{
			this.BaseName = @base;
			this.DisplayName = display;
		}

		public Attribute Convert()
			=> new UnitAttribute(Base)
			{
				Display = Display,
				BaseName = BaseName,
				DisplayName = DisplayName,
				DisplayAsString = DisplayAsString,
				ForceDisplayUnit = ForceDisplayUnit
			};
	}
}
