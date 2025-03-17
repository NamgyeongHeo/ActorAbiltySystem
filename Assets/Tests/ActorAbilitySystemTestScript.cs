using ActorAbilitySystem;
using NUnit.Framework;

public class ActorAbilitySystemTestScript
{
    // A Test behaves as an ordinary method
    [Test]
    public void InstantEffectPasses()
    {
        Character playerCharacter = new Character();
        Character enemy = new Character();
        AbilityComponent playerAbilityComponent = playerCharacter.GetAbilityComponent();
        AbilityComponent enemyAbilityComponent = enemy.GetAbilityComponent();

        float enemyHealthBeforeAttack = enemy.HealthAttribute.CurrentValue;

        // Player Character got a AttackAbility.
        // If this ability is activated, give damage by 30 to target.
        playerAbilityComponent.AddAbility<AttackAbility>();

        // Player Character attack, target is enemy.
        playerAbilityComponent.ActivateByAbilityEvent(new AttackEvent(enemyAbilityComponent));

        float enemyHealthAfterAttack = enemy.HealthAttribute.CurrentValue;
        Assert.IsTrue((enemyHealthBeforeAttack - 30) == enemyHealthAfterAttack);
    }

    [Test]
    public void TemporaryEffectPasses()
    {
        Character playerCharacter = new Character();
        AbilityComponent abilityComponent = playerCharacter.GetAbilityComponent();
        float oldSpeedValue = playerCharacter.SpeedAttribute.CurrentValue;

        const int BuffCount = 5;
        const float Magnitude = 10;
        EffectHandle handle = default;
        for (int i = 0; i < BuffCount; i++)
        {
            handle = abilityComponent.ApplyEffect(new SpeedBuffEffect(Magnitude), abilityComponent);
        }

        Assert.IsTrue((oldSpeedValue + (BuffCount * Magnitude)) == playerCharacter.SpeedAttribute.CurrentValue);

        abilityComponent.RemoveEffect(handle);

        Assert.IsTrue(oldSpeedValue == playerCharacter.SpeedAttribute.CurrentValue);
    }
}
