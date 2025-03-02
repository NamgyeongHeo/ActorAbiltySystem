using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

public abstract partial class ActorEffect : IDisposable
{
    internal event Action onUpdate;
    public event Action<int> onStackCountChange;

    public enum ELifeTimePolicy
    {
        Permanant, // Affect to base value, and effect instance is destroy.
        Temporary, // Affect to current value, and effect instance has life time by duration.
        //Infinite, // Affect to current value, and effect instance is remain infinitely until remove instance manually.
    }

    public enum EStackDurationPolicy
    {
        None, // Don't change exist effect's duration.
        Refresh, // Reset effect's duration.
        Append, // Add new effect's duration to exist effect's duration.
        Individual // Duration timer works individually.
    }

    public enum EStackExpirationPolicy
    {
        Clear, // Clear all stack on expiration.
        Refresh // Recalculate stack and refresh duration.
    }

    private readonly int priority = 0;
    public int Priority
    {
        get
        {
            return priority;
        }
    }

    private AbilityComponent instigator;
    public AbilityComponent Instigator
    {
        get
        {
            return instigator;
        }
    }

    private AbilityComponent target;
    public AbilityComponent Target
    {
        get
        {
            return target;
        }
    }

    public bool DidApply
    {
        get
        {
            return instigator != null && target != null;
        }
    }

    private readonly GameplayTagContainer tagContainer;
    public GameplayTagContainer TagContainer
    {
        get
        {
            return new GameplayTagContainer(tagContainer);
        }
    }

    private readonly GameplayTag[] targetAttributeTags;
    public ReadOnlyCollection<GameplayTag> TargetAttributeTags
    {
        get
        {
            return new ReadOnlyCollection<GameplayTag>(targetAttributeTags);
        }
    }

    private readonly Type[] grantAbilityTypes;
    public ReadOnlyCollection<Type> GrantAbilityTypes
    {
        get
        {
            return new ReadOnlyCollection<Type>(grantAbilityTypes);
        }
    }

    private readonly EStackDurationPolicy stackDurationPolicy;
    public EStackDurationPolicy StackDurationPolicy
    {
        get
        {
            return stackDurationPolicy;
        }
    }

    private readonly EStackExpirationPolicy stackExpirationPolicy;
    public EStackExpirationPolicy StackExpirationPolicy
    {
        get
        {
            return stackExpirationPolicy;
        }
    }

    private readonly bool allowStack = false;
    public bool AllowStack
    {
        get
        {
            return allowStack;
        }
    }

    private readonly ELifeTimePolicy durationPolicy;
    public ELifeTimePolicy DurationPolicy
    {
        get
        {
            return durationPolicy;
        }
    }

    public virtual float Duration
    {
        get
        {
            return 0f;
        }
    }

    private bool isPendingRemove = false;
    public bool IsPendingRemove
    {
        get
        {
            return isPendingRemove;
        }
        internal set
        {
            isPendingRemove = value;
        }
    }

    private List<AbilityHandle> grantAbilityHandles;

    protected bool disposed;
    public bool Disposed
    {
        get
        {
            return disposed;
        }
    }

    public ActorEffect()
    {
        grantAbilityHandles = new List<AbilityHandle>();

        Type type = GetType();
        PriorityAttribute priorityAttribute = type.GetCustomAttribute<PriorityAttribute>();
        if (priorityAttribute != null)
        {
            priority = priorityAttribute.Priority;
        }

        LifeTimePolicyAttribute durationPolicyAttribute = type.GetCustomAttribute<LifeTimePolicyAttribute>();
        if (durationPolicyAttribute != null)
        {
            durationPolicy = durationPolicyAttribute.Policy;
        }

        StackPolicyAttribute stackPolicyAttribute = type.GetCustomAttribute<StackPolicyAttribute>();
        if (stackPolicyAttribute != null)
        {
            allowStack = true;
            stackDurationPolicy = stackPolicyAttribute.DurationPolicy;
            stackExpirationPolicy = stackPolicyAttribute.ExpirationPolicy;
        }

        List<string> tagNames = new List<string>();
        IEnumerable<GameplayTagsAttribute> tagsAttributes = type.GetCustomAttributes<GameplayTagsAttribute>();
        foreach (GameplayTagsAttribute tagsAttribute in tagsAttributes)
        {
            if (tagsAttribute.TagNames != null)
            {
                foreach (string tagName in tagsAttribute.TagNames)
                {
                    tagNames.Add(tagName);
                }
            }
        }
        tagContainer = GameplayTagContainer.Create(tagNames.ToArray());

        List<GameplayTag> targetAttributeTags = new List<GameplayTag>();
        IEnumerable<TargetActorAttributeAttribute> targetActorAttributes = type.GetCustomAttributes<TargetActorAttributeAttribute>();
        foreach (TargetActorAttributeAttribute targetAttribute in targetActorAttributes)
        {
            ReadOnlyCollection<string> targetTagNames = targetAttribute.TagNames;
            if (targetTagNames == null)
            {
                continue;
            }

            foreach (string targetTagName in targetTagNames)
            {
                GameplayTag targetTag = GameplayTag.Create(targetTagName);
                if (targetTag.IsValid())
                {
                    targetAttributeTags.Add(targetTag);
                }
            }
        }
        this.targetAttributeTags = targetAttributeTags.Distinct().ToArray();

        List<Type> grantAbilityTypes = new List<Type>();
        IEnumerable<GrantAbilityAttribute> grantAbilityAttributes = type.GetCustomAttributes<GrantAbilityAttribute>();
        foreach (GrantAbilityAttribute grantAbilityAttribute in grantAbilityAttributes)
        {
            ReadOnlyCollection<Type> abilityTypes = grantAbilityAttribute.AbilityTypes;
            if (abilityTypes == null)
            {
                continue;
            }

            foreach (Type grantAbilityType in abilityTypes)
            {
                if (!typeof(Ability).IsAssignableFrom(grantAbilityType))
                {
                    continue;
                }

                grantAbilityTypes.Add(grantAbilityType);
            }
        }
        this.grantAbilityTypes = grantAbilityTypes.Distinct().ToArray();
    }

    ~ActorEffect()
    {
        Dispose(false);
    }

    internal void SetTarget(AbilityComponent instigator, AbilityComponent target)
    {
        this.instigator = instigator;
        this.target = target;
    }

    protected internal virtual bool CanApply()
    {
        return true;
    }

    protected internal virtual bool IsDirty()
    {
        return false;
    }

    protected internal virtual bool IsNeedRemoveOnApply(ActorEffect effect)
    {
        return false;
    }

    internal int UpdateStackCount()
    {
        int originStack = stack;
        int newStack = RecalculateStackCountOnExpiration();
        if (originStack != newStack) 
        {
            onStackCountChange?.Invoke(newStack);
        }

        return newStack;
    }

    protected virtual int RecalculateStackCountOnExpiration()
    {
        return Stack - 1;
    }

    internal virtual void Construct()
    {
    }

    internal virtual void InitInternal()
    {
        if (durationPolicy != ELifeTimePolicy.Permanant)
        {
            foreach (Type grantAbilityType in grantAbilityTypes)
            {
                grantAbilityHandles.Add(Target.AddAbility(grantAbilityType));
            }
        }

        Init();
    }

    protected virtual void Init()
    {
    }

    internal void DisposeInternal()
    {
        foreach (AbilityHandle grantAbilityHandle in grantAbilityHandles)
        {
            Target.RemoveAbility(grantAbilityHandle);
        }

        Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {

            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected void SetDirty()
    {
        onUpdate?.Invoke();
    }

    protected internal virtual float Modify(float baseValue, float currentValue)
    {
        return currentValue;
    }
}

public class ActorEffect<T> : ActorEffect
{
    private T contextData;
    protected T ContextData
    {
        get
        {
            return contextData;
        }
    }

    internal void ConstructWithData(T contextData)
    {
        this.contextData = contextData;
    }

    internal override void Construct()
    {
        contextData = ConstructDefaultData();
    }

    internal override void StackEffect(ActorEffect newEffect)
    {
        base.StackEffect(newEffect);

        if (newEffect is ActorEffect<T> newEffectWithContext)
        {
            contextData = StackContextData(ContextData, newEffectWithContext.ContextData);
        }
    }

    protected virtual T ConstructDefaultData()
    {
        return default;
    }

    protected virtual T StackContextData(T existData, T newData)
    {
        return newData;
    }
}