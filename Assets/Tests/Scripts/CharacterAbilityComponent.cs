public class CharacterAbilityComponent : AbilityComponent
{
    [ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Health, 100)]
    private ActorAttribute healthAttribute;
    public ActorAttribute HealthAttribute
    {
        get
        {
            return healthAttribute;
        }
    }

    [ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_AttackPower, 30)]
    private ActorAttribute attackPowerAttribute;
    public ActorAttribute AttackPowerAttribute
    {
        get
        {
            return attackPowerAttribute;
        }
    }

    [ActorAttribute.Initialize(GameplayTagsListConst.Character_Stat_Speed, 50)]
    private ActorAttribute speedAttribute;
    public ActorAttribute SpeedAttribute
    {
        get
        {
            return speedAttribute;
        }
    }

    public CharacterAbilityComponent(IAbilityOwnable owner, ITimerHandler timerHandler = null) : base(owner, null, timerHandler)
    {

    }
}