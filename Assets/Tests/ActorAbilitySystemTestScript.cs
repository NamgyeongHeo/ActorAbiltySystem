using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
