public partial class AttackAbility : Ability, IActivateAbilityEventListener<AttackEvent>
{
    public void Activate(AttackEvent abilityEvent)
    {
        DamageEffect damageEffect = new DamageEffect(30);
        abilityEvent.Target.ApplyEffect(damageEffect, OwnerComponent);
    }
}