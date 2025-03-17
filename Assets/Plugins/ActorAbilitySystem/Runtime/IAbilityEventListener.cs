using System;

public interface IActivateAbilityEventListener<in T> where T : AbilityEvent
{
    internal const string Activate_MethodName = nameof(Activate);

    void Activate(T abilityEvent);
}

public interface ICancelAbilityEventListener<in T> where T : AbilityEvent
{
    internal const string Cancel_MethodName = nameof(Cancel);

    void Cancel(T abilityEvent);
}