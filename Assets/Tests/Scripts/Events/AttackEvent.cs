using UnityEngine;

public class AttackEvent : AbilityEvent
{
    private AbilityComponent target;
    public AbilityComponent Target
    {
        get
        {
            return target;
        }
    }

    public AttackEvent(AbilityComponent target)
    {
        this.target = target;
    }
}
