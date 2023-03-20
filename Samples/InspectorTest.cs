using System;
using Fusumity.Attributes.Specific;
using Fusumity.Collections;
using UnityEngine;

namespace Fusumity.Samples
{
	public class InspectorTest : MonoBehaviour
	{
		public SerializableDictionary<int, string> wtf;

		[ReferenceSelection, SerializeReference]
		public ITest test;

		public Spell spell;

		[DisableIf("a"), EnableIf("b"), EnableIf("c")]
		public bool t;
		public bool a;
		public bool b;
		public bool c;

		public bool T()
		{
			return true;
		}
	}

	public interface ITest
	{
	}

	[Serializable]
	public class Tst : ITest
	{
		[DisableIf("c")] public int a;
		[EnableIf("c")] public float b;
		public bool c;
	}

	[Serializable]
	public class Tst1 : ITest
	{
		[Minimum("min"), Maximum("max"), Button("IncrementMax")]
		public int a;

		public int min;
		public int max;

		public void IncrementMax()
		{
			max++;
		}
	}

#region OOP_Exemple

	[Serializable]
	public class Spell
	{
		public string prefabViewKey;

		[ReferenceSelection, SerializeReference]
		public MovementPattern movementPattern;

		public TimePattern timePattern;

		[ReferenceSelection, SerializeReference]
		public HitForm hitForm;
	}

	public abstract class HitForm
	{
	}

	public class CircleHitForm
	{
		[Minimum(0)] public float radius;
	}

	[Serializable]
	public abstract class MovementPattern
	{
		public PeriodicAction[] periodicActions;
		public DistanceAction[] distanceActions;

		public Location location;

		public enum Location
		{
			World,
			Owner,
			Source,
		}

		[Serializable]
		public class PeriodicAction
		{
			[Minimum(0)] public float offset;
			[Minimum(0)] public float period;

			[ReferenceSelection, SerializeReference]
			public SpellAction action;
		}

		[Serializable]
		public class DistanceAction
		{
			[Minimum(0)] public float distance;

			[ReferenceSelection, SerializeReference]
			public SpellAction action;
		}
	}

	[Serializable]
	public class NoMovement : MovementPattern
	{
	}

	[Serializable]
	public class ForwardMovement : MovementPattern
	{
		[Minimum(0)] public float movementSpeed;

		[ReferenceSelection, SerializeReference]
		public SteeringPattern steeringPattern;
	}

	[Serializable]
	public abstract class SteeringPattern
	{
	}

	[Serializable]
	public class NoSteering : SteeringPattern
	{
	}

	[Serializable]
	public class AngleSteering : SteeringPattern
	{
		[AngleToRad] public float radPerSecond;
	}

	[Serializable]
	public class TargetAngleSteering : AngleSteering
	{
		public TargetBhv targetBhv;

		public enum TargetBhv
		{
			NearestTarget,
			RandomTarget,
		}
	}

	[Serializable]
	public class TimePattern
	{
		public PeriodicAction[] periodicActions;
		public TimeAction[] timeActions;
		[DisableIf("infinityDuration")] public DurationAction[] durationActions;

		public bool infinityDuration;

		[DisableIf("infinityDuration"), Minimum(0)]
		public float duration;

		[Serializable]
		public class PeriodicAction
		{
			[Minimum(0)] public float delay;
			[Minimum(0)] public float period;

			[ReferenceSelection, SerializeReference]
			public SpellAction action;
		}

		[Serializable]
		public class TimeAction
		{
			[Minimum(0)] public float time;

			[ReferenceSelection, SerializeReference]
			public SpellAction action;
		}

		[Serializable]
		public class DurationAction
		{
			[Range(0, 1)] public float durationPercent;

			[ReferenceSelection, SerializeReference]
			public SpellAction action;
		}
	}

	[Serializable]
	public abstract class SpellAction
	{
	}

	[Serializable]
	public class DestroySpell : SpellAction
	{
	}

	[Serializable]
	public class CastPattern : SpellAction
	{
		[ReferenceSelection, SerializeReference]
		public Cast cast;

		[Minimum(1)] public int castCount;
		[Minimum(0)] public int maxExtraCastCount;

		[Minimum(0)] public float castCooldown;
		[Minimum(0)] public float castDuration;
	}

	[Serializable]
	public abstract class Cast
	{
	}

	[Serializable]
	public class ComplexCast : Cast
	{
		[ReferenceSelection, SerializeReference]
		public Cast[] casts;

		public CastBhv bhv;

		public enum CastBhv
		{
			All,
			Random,
			Order,
			RandomOrder,
		}
	}

	[Serializable]
	public abstract class DamageZone : Cast
	{
		[ReferenceSelection, SerializeReference]
		public DisplacementPattern displacementPattern;
	}

	[Serializable]
	public class CircleDamageZone : DamageZone
	{
		[Minimum(0)] public float radius;
	}

	[Serializable]
	public class SegmentDamageZone : DamageZone
	{
		[Minimum(0)] public float radius;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		public float segmentRad;
	}

	[Serializable]
	public class LineDamageZone : DamageZone
	{
		[Minimum(0)] public float length;
		[Minimum(0)] public float width;
	}

	[Serializable]
	public class SpawnSpell : Cast
	{
		[ReferenceSelection, SerializeReference]
		public DisplacementPattern displacementPattern;

		public Spell spellToSpawn;
	}

	[Serializable]
	public abstract class DisplacementPattern
	{
		public Location location;

		public enum Location
		{
			Source,
			Owner,
		}
	}

	[Serializable]
	public class TargetPositionPattern : DisplacementPattern
	{
		[Minimum(0)] public float distance;
		public FindTargetPattern findTargetPattern;
	}

	[Serializable]
	public abstract class RadialPattern : DisplacementPattern
	{
		[Minimum(0)] public float distance;
		[Minimum(0)] public float distanceToSpawnDistance;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		public float minRad;

		[AngleToRad, Minimum("minRad"), Maximum(Mathf.PI * 2)]
		public float maxRad;

		[AngleToRad, Minimum(0), Maximum(Mathf.PI * 2)]
		public float spreadRad;

		public SpreadBhv spreadBhv;

		public enum SpreadBhv
		{
			Random,
			OrderedSegments,
			RandomSegments,
			RandomOrderedSegments,
		}
	}

	[Serializable]
	public class DirectionPattern : RadialPattern
	{
		public DirectionBhv directionBhv;

		public enum DirectionBhv
		{
			RandomDirection,
			ForwardDirection,
		}
	}

	[Serializable]
	public class TargetDirectionPattern : RadialPattern
	{
		public FindTargetPattern findTargetPattern;
	}

	[Serializable]
	public class FindTargetPattern
	{
		public TargetBhv targetBhv;
		public TargetExceptionBhv targetExceptionBhv;

		public enum TargetBhv
		{
			NearestTarget,
			RandomTarget,
		}

		public enum TargetExceptionBhv
		{
			AllowSameTarget,
			DisallowSameTarget,
			AvoidSameTarget,
		}
	}

#endregion
}