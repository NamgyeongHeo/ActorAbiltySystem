public partial class AttackAbility : Ability
{
    protected override void ProcessActivateAbilityEvent(AbilityEvent abilityEvent)
    {
        if (abilityEvent is AttackEvent)
        {
            Activate(abilityEvent as AttackEvent);
        }
    }

    protected override void ProcessCancelAbilityEvent(AbilityEvent abilityEvent)
    {
    }
}
