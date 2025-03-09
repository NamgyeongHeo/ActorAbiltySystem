[TargetActorAttribute(GameplayTagsListConst.Character_Stat_Health)]
public class DamageEffect : ActorEffect
{
    private readonly float magnitude;
    
    public DamageEffect(float magnitude)
    {
        this.magnitude = magnitude;
    }

    protected override float Modify(float baseValue, float currentValue)
    {
        return currentValue + magnitude;
    }
}