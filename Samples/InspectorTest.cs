using System;
using Fusumity.Attributes.Specific;
using Fusumity.Collections;
using UnityEngine;

namespace Fusumity.Samples
{
	internal enum EnumType
	{
		A,
		B,
		C,
		D,
		F,
		G,
	}

	internal class InspectorTest : MonoBehaviour
	{
		internal ReorderableEnumArray<EnumType> enumEnumArray;
		internal ReorderableEnumArray<EnumType, int> reorderableEnumArray;
		internal EnumArray<EnumType, int> enumArray;

		internal SerializableDictionary<int, string> wtf;

		[ReferenceSelection, SerializeReference]
		internal ITest test;

		internal Spell spell;

		[DisableIf("a"), EnableIf("b"), EnableIf("c")]
		internal bool t;
		internal bool a;
		internal bool b;
		internal bool c;

		internal bool T()
		{
			return true;
		}
	}

	internal interface ITest
	{
	}

	[Serializable]
	internal class Tst : ITest
	{
		[DisableIf("c")] internal int a;
		[EnableIf("c")] internal float b;
		internal bool c;
	}

	[Serializable]
	internal class Tst1 : ITest
	{
		[Minimum("min"), Maximum("max"), Button("IncrementMax")]
		internal int a;

		internal int min;
		internal int max;

		internal void IncrementMax()
		{
			max++;
		}
	}

#region OOP_Exemple

	[Serializable]
	internal class Spell
	{
		internal string prefabViewKey;

		[ReferenceSelection, SerializeReference]
		internal MovementPattern movementPattern;

		internal TimePattern timePattern;

		[ReferenceSelection, SerializeReference]
		internal HitForm hitForm;
	}

	internal abstract class HitForm
	{
	}

	internal class CircleHitForm
	{
		[Minimum(0)] internal float radius;
	}

	[Serializable]
	internal abstract class MovementPattern
	{
		internal PeriodicAction[] periodicActions;
		internal DistanceAction[] distanceActions;

		internal Location location;

		internal enum Location
		{
			World,
			Owner,
			Source,
		}

		[Serializable]
		internal class PeriodicAction
		{
			[Minimum(0)] internal float offset;
			[Minimum(0)] internal float period;

			[ReferenceSelection, SerializeReference]
			internal SpellAction action;
		}

		[Serializable]
		internal class DistanceAction
		{
			[Minimum(0)] internal float distance;

			[ReferenceSelection, SerializeReference]
			internal SpellAction action;
		}
	}

	[Serializable]
	internal class NoMovement : MovementPattern
	{
	}

	[Serializable]
	internal class ForwardMovement : MovementPattern
	{
		[Minimum(0)] internal float movementSpeed;

		[ReferenceSelection, SerializeReference]
		internal SteeringPattern steeringPattern;
	}

	[Serializable]
	internal abstract class SteeringPattern
	{
	}

	[Serializable]
	internal class NoSteering : SteeringPattern
	{
	}

	[Serializable]
	internal class AngleSteering : SteeringPattern
	{
		[AngleToRad] internal float radPerSecond;
	}

	[Serializable]
	internal class TargetAngleSteering : AngleSteering
	{
		internal TargetBhv targetBhv;

		internal enum TargetBhv
		{
			NearestTarget,
			RandomTarget,
		}
	}

	[Serializable]
	internal class TimePattern
	{
		internal PeriodicAction[] periodicActions;
		internal TimeAction[] timeActions;
		[DisableIf("infinityDuration")] internal DurationAction[] durationActions;

		internal bool infinityDuration;

		[DisableIf("infinityDuration"), Minimum(0)]
		internal float duration;

		[Serializable]
		internal class PeriodicAction
		{
			[Minimum(0)] internal float delay;
			[Minimum(0)] internal float period;

			[ReferenceSelection, SerializeReference]
			internal SpellAction action;
		}

		[Serializable]
		internal class TimeAction
		{
			[Minimum(0)] internal float time;

			[ReferenceSelection, SerializeReference]
			internal SpellAction action;
		}

		[Serializable]
		internal class DurationAction
		{
			[Range(0, 1)] internal float durationPercent;

			[ReferenceSelection, SerializeReference]
			internal SpellAction action;
		}
	}

	[Serializable]
	internal abstract class SpellAction
	{
	}

	[Serializable]
	internal class DestroySpell : SpellAction
	{
	}

	[Serializable]
	internal class CastPattern : SpellAction
	{
		[ReferenceSelection, SerializeReference]
		internal Cast cast;

		[Minimum(1)] internal int castCount;
		[Minimum(0)] internal int maxExtraCastCount;

		[Minimum(0)] internal float castCooldown;
		[Minimum(0)] internal float castDuration;
	}

	[Serializable]
	internal abstract class Cast
	{
	}

	[Serializable]
	internal class ComplexCast : Cast
	{
		[ReferenceSelection, SerializeReference]
		internal Cast[] casts;

		internal CastBhv bhv;

		internal enum CastBhv
		{
			All,
			Random,
			Order,
			RandomOrder,
		}
	}

	[Serializable]
	internal abstract class DamageZone : Cast
	{
		[ReferenceSelection, SerializeReference]
		internal DisplacementPattern displacementPattern;
	}

	[Serializable]
	internal class CircleDamageZone : DamageZone
	{
		[Minimum(0)] internal float radius;
	}

	[Serializable]
	internal class SegmentDamageZone : DamageZone
	{
		[Minimum(0)] internal float radius;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		internal float segmentRad;
	}

	[Serializable]
	internal class LineDamageZone : DamageZone
	{
		[Minimum(0)] internal float length;
		[Minimum(0)] internal float width;
	}

	[Serializable]
	internal class SpawnSpell : Cast
	{
		[ReferenceSelection, SerializeReference]
		internal DisplacementPattern displacementPattern;

		internal Spell spellToSpawn;
	}

	[Serializable]
	internal abstract class DisplacementPattern
	{
		internal Location location;

		internal enum Location
		{
			Source,
			Owner,
		}
	}

	[Serializable]
	internal class TargetPositionPattern : DisplacementPattern
	{
		[Minimum(0)] internal float distance;
		internal FindTargetPattern findTargetPattern;
	}

	[Serializable]
	internal abstract class RadialPattern : DisplacementPattern
	{
		[Minimum(0)] internal float distance;
		[Minimum(0)] internal float distanceToSpawnDistance;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		internal float minRad;

		[AngleToRad, Minimum("minRad"), Maximum(Mathf.PI * 2)]
		internal float maxRad;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		internal float spreadRad;

		internal SpreadBhv spreadBhv;

		internal enum SpreadBhv
		{
			Random,
			OrderedSegments,
			RandomSegments,
			RandomOrderedSegments,
		}
	}

	[Serializable]
	internal class DirectionPattern : RadialPattern
	{
		internal DirectionBhv directionBhv;

		internal enum DirectionBhv
		{
			RandomDirection,
			ForwardDirection,
		}
	}

	[Serializable]
	internal class TargetDirectionPattern : RadialPattern
	{
		internal FindTargetPattern findTargetPattern;
	}

	[Serializable]
	internal class FindTargetPattern
	{
		internal TargetBhv targetBhv;
		internal TargetExceptionBhv targetExceptionBhv;

		internal enum TargetBhv
		{
			NearestTarget,
			RandomTarget,
		}

		internal enum TargetExceptionBhv
		{
			AllowSameTarget,
			DisallowSameTarget,
			AvoidSameTarget,
		}
	}

#endregion
}