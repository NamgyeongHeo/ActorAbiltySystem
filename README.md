# ActorAbilitySystem
## Table of Contents
 - [Intro](#intro) 
 - [Install](#install)
 - [How to use](#how-to-use)
	- [1. AbilityComponent](#1-abilitycomponent)
		 - [1.1. Setup and Initialization](#11-setup-and-initialization)
	- [2. ActorAbility](#2-actorability)
		- [2.1. Add and remove](#21-add-and-remove)
		- [2.2 Initialization](#22-initialization)
		- [2.3. Activate and Cancel](#23-activate-and-cancel)
		- [2.4. AbilityEvent](#24-abilityevent)
	- [3. ActorAttribute](#3-actorattribute)
		- [3.1. Initialization](#31-initialization)
		- [3.2. Get or find ActorAttribute](#32-get-or-find-actorattribute)
		- [3.3. Base value and Current value](#33-base-value-and-current-value)
		- [3.4. Observe attribute value change](#34-observe-attribute-value-change)
	- [4. ActorEffect](#4-actoreffect)
		- [4.1. Modifying ActorAttribute](#41-modifying-actorattribute)
		- [4.2. Apply ActorEffect](#42-apply-actoreffect)
		- [4.3. TemporaryActorEffect](#43-temporaryactoreffect)
		- [4.4. Add tag to ActorEffect](#44-add-tag-to-actoreffect)
		- [4.5. Priority of TemporaryActorEffect](#45-priority-of-temporaryactoreffect)
 

## Intro 
This unity package provides AbilitySystem for managing actor's ability and states.

In this context, actor is meaning an object that exists in the world.

You can use these features
 - Add and remove ability to actor.
 - Store and calculate actor's attributes.
 - Change attribute or state with effect.

## Install
You need to add ScopedRegistry to `manifest.json`
```
"scopedRegistries": [
  {
    "name": "QuestionPackages", // You can change it to any name you want.
    "url": "https://registry.npmjs.org/",
    "scopes": [
      "com.question"
    ],
    "overrideBuiltIns": false
  }
]
```

Then you can add `https://github.com/NamgyeongHeo/ActorAbiltySystem.git?path=Assets/Plugins/ActorAbilitySystem` to UPM via git url.

## How to use
Before read this contents, I recommend you check [GameplayTags]("https://github.com/NamgyeongHeo/GameplayTags.git") package.

`GameplayTags` is used to identify data.

### 1. AbilityComponent
`AbilityComponent` manages data of `ActorAbilitySystem`.

### 1.1. Setup and Initialization
You can intialize `AbilityComponent` with constructor.

```c#
public class Character : IAbilityOwnable
{
	private readonly AbilityComponent abilityComponent;

	public Character()
	{
		GameplayTag healthTag = GameplayTag.Create(GameplayTagsListConst.Character_Stat_Health);
		GameplayTag attackPowerTag = GameplayTag.Create(GameplayTagsListConst.Character_Stat_AttackPower);

		abilityComponent = new AbilityComponent(this, // Set owner of the AbilityComponent to this instance.
            new AttributeInitializationInfo[] // Add AttributeInitializationInfo for initialize ActorAttribute data.
            {
                new AttributeInitializationInfo(healthTag, 100),
                new AttributeInitializationInfo(attackPowerTag, 10)
            }
        );
	}

	public AbilityComponent GetAbilityComponent()
	{
	    return abilityComponent;
	}
}
```

### 2. ActorAbility
`ActorAbility` is the abstract class for defining the behavior of actors.

### 2.1. Add and remove
`AbilityComponent` can add or remove `ActorAbility`.
```c#
private Character playerCharacter;
private AbilityHandle jumpAbilityHandle;

private void OnEnable()
{
	AbilityComponent abilityComponent = playerCharacter.GetAbilityComponent();
	
	// AddAbility() Adds an ability based on the given type.
	// It returns an AbilityHandle instance.
	jumpAbilityHandle = abilityComponent.AddAbility<JumpAbility>();
}

private void OnDisable()
{
	AbilityComponent abilityComponent = playerCharacter.GetAbilityComponent();

	// RemoveAbility() removes the ability corresponding to the AbilityHandle instance.
	abilityComponent.RemoveAbility(jumpAbilityHandle);
}
```

### 2.2. Initialization
The child class of `ActorAbility` must have a default constructor.

If you want to access `Owner` and `OwnerComponent`, you can override `Init()` function.

```c#
private Character ownerCharacter;
private CharacterAbilityComponent ownerComponent;

protected override void Init()
{
    ownerCharacter = Owner as Character;
    ownerComponent = OwnerComponent as CharacterAbilityComponent;
}
```

### 2.3. Activate and Cancel
`AbilityComponent` can activate and cancel manually via AbilityHandle instance.

You can override `Activate()` and `Cancel()` functions to implement behavior of `ActorAbility`.

```c#
public class RunAbility : ActorAbility
{
    private Character character;
    private ActorAttribute speedAttribute;

    protected override void Init()
    {
        character = Owner as Character;
        speedAttribute = OwnerComponent.GetAttribute(GameplayTag.Create(GameplayTagsListConst.Character_Stat_Speed));
    }

    protected override void Activate()
    {
        if (character == null)
        {
	        Finish();
            return;
        }

        character.StartRun(speedAttribute.CurrentValue);

        Finish();
    }

    protected override void Cancel()
    {
        if (character == null)
        {
            return;
        }

        character.StopRun();
    }
}
```

Then you can activate or cancel manually via `AbilityComponent`.
```c#
private AbilityComponent abilityComponent;
private AbilityHandle runAbilityHandle;

private void Awake()
{
	abilityComponent = new AbilityComponent(this);
	runAbilityHandle = abilityComponent.AddAbility<RunAbility>();
}

// Call when player press Run action binding key.
public void OnRunPress()
{
	abilityComponent.ActivateAbility(runAbilityHandle); // Activate RunAbility
}

// Call when player release Run action binding key.
public void OnRunRelease()
{
	abilityComponent.CancelAbility(runAbilityHandle); // Cancel RunAbility
}
```

### 2.4. AbilityEvent
`AbilityEvent` is a data class for activating or canceling abilities by situations.

```c#
public class AttackEvent : AbilityEvent
{
    private readonly AbilityComponent target;
    public AbilityComponent Target
    {
        get
        {
            return target;
        }
    }

    private readonly float damage;
    public float Damage
    {
        get
        {
            return damage;
        }
    }

    public AttackEvent(AbilityComponent target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }
}
```
And you can activate or cancel abilities with `AbilityEvent`.

```c#
AttackEvent attackEvent = new AttackEvent(enemy.GetAbilityComponent(), 30f);
abilityComponent.ActivateByEvent(attackEvent);
```

Then, it activates abilities that implement `IActivateAbilityEventListener<DamageEvent>`.

```c#
public partial class AttackAbility : ActorAbility, IActivateAbilityEventListener<DamageEvent>
{
    public void Activate(AttackEvent abilityEvent)
    {
        DamageEffect damageEffect = new DamageEffect(abilityEvent.magnitude);
        abilityEvent.Target.ApplyEffect(damageEffect, OwnerComponent);
        Finish();
    }
}
```
### 3. ActorAttribute
`ActorAttribute` can contain Actor's attribute with `GameplayTag`.

### 3.1. Initialization
You can define and initialize with two case.

Which one is using `AttributeInitializationInfo` struct.
```c#
public class Actor : MonoBehaviour, IAbilityOwnable
{
	[SerializeField] // You can expose parameters in editor
	private AttributeInitializationInfo[] attributeInitializationInfos;

	private AbilityComponent abilityComponent;

	private void Awake()
	{
		abilityComponent = new AbilityComponent(this, attributeInitializationInfos);
	}
	
	public AbilityComponent GetAbilityComponent()
	{
		return abilityComponent;
	}
}
```

And another one is using `InitializeAttribute` in `AbilityComponent` class field.
```c#
public class PlayerCharacterAbilityComponent : AbilityComponent
{
	// Player Character has AttackPower attribute. Initial value is 50.
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_AttackPower, 50.0f)]
	private ActorAttribute attackPowerAttribute;

	// Also Player Character has Speed attribute;
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Speed, 10.0f)]
	private ActorAttribute speedAttribute;

	// ...
}
```

### 3.2. Get or find ActorAttribute
You can access `ActorAttribute` by `AbilityComponent`.

```c#
ActorAttribute GetHealthAttribute(AbilityComponent abilityComponent)
{
	GameplayTag healthAttribute = GameplayTag.Create(GameplayTagsListConst.Character_Stat_Health);
	return abilityComponent.GetAttribute(healthAttribute); // Get health attribute
}

ActorAttribute[] GetAllStatAttributes(AbilityComponent abilityComponent)
{
	GameplayTag statTag = GameplayTag.Create(GameplayTagsListConst.Character_Stat);

	// Find attributes by matching Character.Stat tag
	return abilityComponent.FindAttributes((GameplayTag attributeTag) => attributeTag.Match(statTag));
}
```


### 3.3. Base value and Current value
`ActorAttribute` has two value. Base value and Current value.

The Base value is the value to which `TemporaryActorEffect` instances are not applied.
Current value is the value applied by `TemporaryActorEffect` instances, unlike the above.

```c#
// Slow Character's speed.
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Speed)]
public class SlowEffect : TemporaryActorEffect
{
	private magnitude;
	
	public SlowEffect(float magnitude)
	{
		this.magnitude = magnitude;
	}

	protected override Modify(float baseValue, float currentValue)
	{
		return currentValue - magnitude;
	}
}

// Apply damage to Character's health.
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)]
public class DamageEffect ; ActorEffect
{
	private float magnitude;

	public DamageEffect(float magnitude)
	{
		this.magnitude = magnitude;
	}
	
	protected override Modify(float baseValue, float currentValue)
	{
		return baseValue - magnitude;
	}
}

public class PlayerCharacterAbilityComponent : AbilityComponent
{
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Speed, 10.0f)]
	private ActorAttribute speedAttribute;
	
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Speed, 100.0f)]
	private ActorAttribute healthAttribute;

	// Trap cause slow debuff and damage.
	public void OnCaughtInTrap(AbilityComponent trapAbilityComponent)
	{
		PrintAttributeValue(healthAttribute); // 100 100
		ApplyEffect(new DamageEffect(10.0f), trabAbilityComponent);
		PrintAttributeValue(healthAttribute); // 90 90

		PrintAttributeValue(speedAttribute); // 10, 10
		ApplyEffect(new SlowEffect(5.0f), trapAbilityComponent);
		PrintAttributeValue(speedAttribute); // 10, 5
	}

	private void PrintAttributeValue(ActorAttribute actorAttribute)
	{
		Debug.Log($"{actorAttribute.GetBaseValue()}, {actorAttribute.GetCurrentValue()}");
	}
}
```

### 3.4. Observe attribute value change
You can observe attribute value change with `ActorAttribute.onAttributeUpdate` event.

```c#
public class HealthStat : MonoBehaviour
{
	[SerializeField]
	private UnityEngine.UI.Text healthStatTxt;

	private PlayerCharacterAbilityComponent targetAbilityComponent;

	public void SetTarget(PlayerCharacterAbilityComponent abilityComponent)
	{
		if (targetAbilityComponent != null)
		{
			targetAbilityComponent.healthAttribute.onAttributeUpdate -= OnAttributeUpdate;
		}
		
		targetAbilityComponent = abilityComponent;

		if (targetAbilityComponent != null)
		{
			targetAbilityComponent.healthAttribute.onAttributeUpdate += OnAttributeUpdate;
		}
	}
	
	private void OnAttributeUpdate(float baseValue, float oldValue, float currentValue)
	{
		healthStatTxt.text = $"Health : {currentValue}";
	}
}
```

### 4. ActorEffect
`ActorEffect` class is the abstract class for modify `ActorAttribute`'s value.

### 4.1. Modifying ActorAttribute
When `ActorEffect` applied, it modify target `ActorAttribute`'s base value.

For define how modify attribute value, `ActorEffect`'s child classes need override `ActorEffect.Modify()` function.

Also, `ActorEffect` must set target to select `ActorAttribute` to modify.
```c#
// DamageEffect modify character's health
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)] 
public class DamageEffect : ActorEffect
{
	private float magnitude;

	public DamageEffect(float magnitude)
	{
		this.magnitude = magnitude;
	}

	// Subtract character health by magnitude
	protected override float Modify(float baseValue, float currentValue)
	{
		return baseValue - magnitude;
	}
}
```

### 4.2. Apply ActorEffect
`ActorEffect` can apply with `AbilityComponent.ApplyEffect()`.

```c#
public class Sword : MonoBehaviour, IAbilityOwnable
{
	private AbilityComponent abilityComponent;
	private ActorAttribute damageAttribute;

	[SerializeField]
	private float damage = 5.0f;

	private void Awake()
	{
		GameplayTag weaponDamageTag = GameplayTag.Create(GameplayTagsListConst.Weapon_Stat_Damage);
		abilityComponent = new AbilityComponent(new AttributeInitializationInfo[]
		{
			new AttributeInitializationInfo(weaponDamageTag, damage)
		});

		damageAttribute = abilityComponent.GetAttribute(weaponDamageTag);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.TryGetComponent<IAbilityOwnable>(out IAbilityOwnable abilityOwnable))
		{
			abilityOwnable.GetAbilityComponent()?.ApplyEffect(new DamageEffect(damageAttribute.GetCurrentValue()), abilityComponent);
		}
	}
	
	public AbilityComponent GetAbilityComponent()
	{
		return abilityComponent;
	}
}
```

### 4.3. TemporaryActorEffect
`TemporaryActorEffect` is modify target `ActorAttribute`'s current value.
```c#
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Speed)]
public class SlowEffect : TemporaryActorEffect
{
	private magnitude;
	
	public SlowEffect(float magnitude)
	{
		this.magnitude = magnitude;
	}

	protected override Modify(float baseValue, float currentValue)
	{
		return currentValue - magnitude;
	}
}

class PlayerCharacterAbilityComponent : AbilityComponent
{
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Speed, 10.0f)]
	private ActorAttribute speedAttribute;

	// Trap cause slow debuff and damage.
	public void OnCaughtInTrap(AbilityComponent trapAbilityComponent)
	{
		PrintAttributeValue(speedAttribute); // 10, 10
		ApplyEffect(new SlowEffect(5.0f), trapAbilityComponent);
		PrintAttributeValue(speedAttribute); // 10, 5
	}

	private void PrintAttributeValue(ActorAttribute actorAttribute)
	{
		Debug.Log($"{actorAttribute.GetBaseValue()}, {actorAttribute.GetCurrentValue()}");
	}
}
```

You can set `TemporaryActorEffect`'s life time.
```c#
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)]
public class PoisonEffect : TemporaryActorEffect
{
	protected override void Init()
	{
		RegisterExpirationTimer(3.0f);
	}
}
```

Or remove manually from `AbilityComponent`.
```c#
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)]
public class PoisonEffect : TemporaryActorEffect
{
	// Don't register expiration timer.
}

public class PlayerCharacterAbilityComponent : AbilityComponent
{
	// Remove poison itself.
	public void Detox()
	{
		RemoveEffect<PoisionEffect>();
	}
}
```


### 4.4. Add tag to ActorEffect
`ActorEffect.GameplayTagsAttribute` add `GameplayTag` to `ActorEffect` instance.
```c#
// Poison effect is a debuff
[ActorEffect.GameplayTags(GameplayTagsListConst.Effect_Debuff_Poison)]
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)]
public class PoisonEffect : TemporaryActorEffect
{
}
```
`AbilityComponent` can check granted tags with `AbilityComponent.TagContainer`.

```
bool HasDebuff(AbilityComponent abilityComponent)
{
	GameplayTag debuffTag = GameplayTag.Create(GameplayTagsListConst.Effect_Debuff);

	// Check abilityComponent has debuff
	return abilityComponent.TagContainer.HasTag(debuffTag);
}
```

### 4.5. Priority of TemporaryActorEffect
`TemporaryActorEffect.PriorityAttribute` set priority of `TemporaryActorEffect`.

```c#
// AddAttackPowerEffect has higher priority than MultiplyAttackPowerEffect
[TemporaryActorEffect.Priority(0)]
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_AttackPower)]
public class MultiplyAttackPowerEffect : TemporaryActorEffect
{
	private float multiplication;

	public MultiplyAttackPowerEffect(float multiplication)
	{
		this.multiplication = multiplication;
	}

	protected override float Modify(float baseValue, float currentValue)
	{
		return currentValue * multiplication;
	}
}

[TemporaryActorEffect.Priority(1)]
[ActorEffect.TargetActorAttribute(GameplayTagsListConst.Character_Stat_AttackPower)]
public class AddAttackPowerEffect : TemporaryActorEffect
{
	private float addition;

	public AddAttackPowerEffect(float addition)
	{
		this.addition = addition;
	}

	protected override float Modify(float baseValue, float currentValue)
	{
		return currentValue + addition;
	}
}

public class PlayerCharacterAbilityComponent : AbilityComponent
{
	[ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_AttackPower, 100.0f)]
	private ActorAttribute attackPowerAttribute;

	public void ApplyAttackPowerEffects()
	{
		PrintAttackPower(); // 100, 100
		
		float multiplication = 1.1f;
		float addition = 10.0f;
		ApplyEffect(new MultiplyAttackPowerEffect(multiplification), this);
		ApplyEffect(new AddAttackPowerEffect(addition), this);

		PrintAttackPower(); // 100, 121 because it works to current value like ((100 + addition) * multiplication)
	}
	
	private void PrintAttackPower()
	{
		Debug.Log($"{attackPowerAttribute.GetBaseValue()}, {attackPowerAttribute.GetCurrentValue()}");
	}
}
```
