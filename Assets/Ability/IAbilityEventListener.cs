public interface IActivateAbilityEventListener<in T> where T : AbilityEvent
{
    void Activate(T abilityEvent);
}

public interface ICancelAbilityEventListener<in T> where T : AbilityEvent
{
    void Cancel(T abilityEvent);
}