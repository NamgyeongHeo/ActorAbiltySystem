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

    public AbilityComponent GetAbilityComponent()
    {
        return abilityComponent;
    }
}