using System;
using System.Reflection;
using UnityEngine;

public abstract class TemporaryActorEffect : ActorEffect
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PriorityAttribute : Attribute
    {
        private int priority;
        public int Priority
        {
            get
            {
                return priority;
            }
        }

        public PriorityAttribute(int priority)
        {
            this.priority = priority;
        }
    }

    private readonly int priority = 0;
    public int Priority
    {
        get
        {
            return priority;
        }
    }

    private int stack = 1;
    protected int Stack
    {
        get
        {
            return stack;
        }
    }

    public TemporaryActorEffect()
    {
        Type type = GetType();
        PriorityAttribute priorityAttribute = type.GetCustomAttribute<PriorityAttribute>();
        if (priorityAttribute != null)
        {
            priority = priorityAttribute.Priority;
        }
    }

    protected TimerHandle RegisterExpirationTimer(float lifeTime)
    {
        if (Target == null)
        {
            Debug.LogError($"{nameof(TemporaryActorEffect)}.{nameof(RegisterExpirationTimer)}() must call after initialization.");
            return TimerHandle.None;
        }

        return Target.TimerManager.Start(Expire, lifeTime);
    }

    protected void UnregisterExpirationTimer(TimerHandle handle)
    {
        if (Target == null)
        {
            Debug.LogError($"{nameof(TemporaryActorEffect)}.{nameof(UnregisterExpirationTimer)}() must call after initialization.");
        }

        Target.TimerManager.Stop(handle, false);
    }

    protected void SetRemainTime(TimerHandle handle, float remainTime)
    {
        if (Target == null)
        {
            Debug.LogError($"{nameof(TemporaryActorEffect)}.{nameof(SetRemainTime)}() must call after initialization.");
        }

        Target.TimerManager.SetRemainTime(handle, remainTime);
    }

    protected void ResetTimer(TimerHandle handle)
    {
        if (Target == null)
        {
            Debug.LogError($"{nameof(TemporaryActorEffect)}.{nameof(ResetTimer)}() must call after initialization.");
        }

        Target.TimerManager.Reset(handle);
    }

    internal void ProcessStack(TemporaryActorEffect newEffect)
    {
        OnStack(newEffect, ref stack);
        SetDirty();
    }

    private void Expire()
    {
        OnExpired(ref stack);
        SetDirty();
    }

    protected void Remove()
    {
        Target.RemoveEffect(GetType());
    }

    // Call when TemporaryActorEffect applied to AbilityComponent already has same type effect.
    protected virtual void OnStack(TemporaryActorEffect newEffect, ref int stack) { stack++; }

    // Call when expiration timer is end.
    protected virtual void OnExpired(ref int stack) { stack--; }
}