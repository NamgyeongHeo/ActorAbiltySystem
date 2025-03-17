using ActorAbilitySystem;

public class Character : IAbilityOwnable
{
    private CharacterAbilityComponent abilityComponent;
    public ActorAttribute HealthAttribute
    {
        get
        {
            return abilityComponent.HealthAttribute;
        }
    }

    public ActorAttribute SpeedAttribute
    {
        get
        {
            return abilityComponent.SpeedAttribute;
        }
    }

    public Character()
    {
        abilityComponent = new CharacterAbilityComponent(this);
    }

    public AbilityComponent GetAbilityComponent()
    {
        return abilityComponent;
    }
}