using ActorAbilitySystem;

[TargetActorAttribute(GameplayTagsListConst.Character_Stat_Speed)]
public class SpeedBuffEffect : TemporaryActorEffect
{
    private readonly float magnitude;

    public SpeedBuffEffect(float magnitude)
    {
        this.magnitude = magnitude;
    }

    protected override float Modify(float baseValue, float currentValue)
    {
        return currentValue + (magnitude * Stack);
    }
}
