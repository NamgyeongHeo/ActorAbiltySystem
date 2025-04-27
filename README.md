# ActorAbilitySystem
## Table of Contents
- [Intro](#Intro) 
- [Install](#Install)

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

### 1.1 Setup and Initialization
You can intialize `AbilityComponent` with constructor.

```
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

### 2.1 Add and remove
`AbilityComponent` can add or remove `ActorAbility`.
```
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

### 2.2 Initialization
The child class of `ActorAbility` must have a default constructor.

If you want to access `Owner` and `OwnerComponent`, you can override `Init()` function.

```
private Character ownerCharacter;
private CharacterAbilityComponent ownerComponent;

protected override void Init()
{
    ownerCharacter = Owner as Character;
    ownerComponent = OwnerComponent as CharacterAbilityComponent;
}
```

### 2.3 Activate and Cancel
`AbilityComponent` can activate and cancel manually via AbilityHandle instance.

You can override `Activate()` and `Cancel()` functions to implement behavior of `ActorAbility`.

```
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
```
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

### 2.4 AbilityEvent
`AbilityEvent` is a data class for activating or canceling abilities by situations.

```
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

```
AttackEvent attackEvent = new AttackEvent(enemy.GetAbilityComponent(), 30f);
abilityComponent.ActivateByEvent(attackEvent);
```

Then, it activates abilities that implement `IActivateAbilityEventListener<DamageEvent>`.

```
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
