using ActorAbilitySystem;

public partial class AttackAbility : ActorAbility, IActivateAbilityEventListener<AttackEvent>
{
    public void Activate(AttackEvent abilityEvent)
    {
        DamageEffect damageEffect = new DamageEffect(30);
        abilityEvent.Target.ApplyEffect(damageEffect, OwnerComponent);
        Finish();
    }
}