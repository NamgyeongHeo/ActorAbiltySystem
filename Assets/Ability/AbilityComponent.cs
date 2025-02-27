using Codice.Client.Common.Connection;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static ActorEffect;

public interface IAbilityOwnable
{
    AbilityComponent GetAbilityComponent();
}

public struct AbilityHandle
{
    private static int maxIndex = 0;

    private int index;
    private WeakReference<Ability> ability;
    public WeakReference<Ability> Ref
    {
        get
        {
            return ability;
        }
    }

    internal static AbilityHandle Generate(Ability ability)
    {
        if (ability == null)
        {
            return new AbilityHandle() { index = -1 };
        }

        return new AbilityHandle() { ability = new WeakReference<Ability>(ability), index = maxIndex++ };
    }

    public bool IsValid()
    {
        return (index >= 0) && (ability != null);
    }
}

public struct EffectHandle
{
    private static int maxIndex;

    private int index;

    private WeakReference<ActorEffect> effect;
    public WeakReference<ActorEffect> Ref
    {
        get
        {
            return effect;
        }
    }

    internal static EffectHandle Generate(ActorEffect effect)
    {
        if (effect == null)
        {
            return new EffectHandle() { index = -1 };
        }

        return new EffectHandle() { effect = new WeakReference<ActorEffect>(effect), index = maxIndex++ };
    }

    public bool IsValid()
    {
        return (index >= 0) && (effect != null);
    }
}

[Serializable]
public struct AttributeInitializationInfo
{
    public GameplayTag attributeIdentifier;
    public float initialValue;
}

public class AbilityComponent
{
    public struct EffectTimerHandle
    {
        public EffectHandle effectHandle;
        public TimerHandle timerHandle;
    }

    private const int WaitFrequency = 25;

    private Dictionary<GameplayTag, ActorAttribute> attributes;

    private Dictionary<AbilityHandle, Ability> abilities;
    private Dictionary<EffectHandle, ActorEffect> effects;
    private List<EffectTimerHandle> effectDurationTimers;

    private TimerManager effectTimerManager;

    private GameplayTagContainer tagContainer;
    public GameplayTagContainer TagContainer
    {
        get
        {
            return tagContainer;
        }
    }

    private IAbilityOwnable owner;
    internal IAbilityOwnable Owner
    {
        get
        {
            return owner;
        }
    }

    private Task abilityRemoveTask;
    private Task effectRemoveTask;

    private CancellationTokenSource removeTokenSource;

    public AbilityComponent(IAbilityOwnable owner, IEnumerable<AttributeInitializationInfo> attributeInitializers)
    {
        removeTokenSource = new CancellationTokenSource();

        tagContainer = GameplayTagContainer.Create();
        effectTimerManager = new TimerManager();

        attributes = new Dictionary<GameplayTag, ActorAttribute>();
        abilities = new Dictionary<AbilityHandle, Ability>();
        effects = new Dictionary<EffectHandle, ActorEffect>();
        effectDurationTimers = new List<EffectTimerHandle>();

        this.owner = owner;

        List<FieldInfo> fields = new List<FieldInfo>();
        Type type = GetType();
        while (type != null)
        {
            fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            type = type.BaseType;
        }

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(ActorAttribute))
            {
                continue;
            }

            ActorAttribute.InitializeAttribute initAttribute = field.GetCustomAttribute<ActorAttribute.InitializeAttribute>();
            if (initAttribute == null)
            {
                continue;
            }

            GameplayTag tag = GameplayTag.Create(initAttribute.TagName);
            if (!tag.IsValid())
            {
                continue;
            }

            ActorAttribute actorAttribute;
            if (attributes.ContainsKey(tag))
            {
                actorAttribute = attributes[tag];
            }
            else
            {
                actorAttribute = ActorAttribute.Create(tag, initAttribute.InitialValue);
                attributes.Add(tag, actorAttribute);
            }

            field.SetValue(this, actorAttribute);
        }

        if (attributeInitializers != null)
        {
            foreach (AttributeInitializationInfo initInfo in attributeInitializers)
            {
                GameplayTag tag = initInfo.attributeIdentifier;
                if (attributes.ContainsKey(tag))
                {
                    continue;
                }

                ActorAttribute attribute = ActorAttribute.Create(tag, initInfo.initialValue);
                if (attribute != null)
                {
                    attributes.Add(tag, attribute);
                }
            }
        }
    }

    ~AbilityComponent()
    {
        removeTokenSource.Cancel();
        removeTokenSource.Dispose();
        removeTokenSource = null;

        effectTimerManager?.Dispose();
        effectTimerManager = null;
    }

    public ActorAttribute GetAttribute(GameplayTag tag)
    {
        if (!attributes.ContainsKey(tag))
        {
            return null;
        }

        return attributes[tag];
    }

    public ActorAttribute[] FindAttributes(Predicate<GameplayTag> predicate)
    {
        if (predicate == null)
        {
            return Array.Empty<ActorAttribute>();
        }

        return attributes.Where((pair) =>
        {
            foreach (Delegate del in predicate.GetInvocationList())
            {
                if (!(bool)del.DynamicInvoke(pair.Key))
                {
                    return false;
                }
            }
            return true;
        }).Select((pair) => pair.Value).ToArray();
    }

    public bool TryGetAttribute(GameplayTag tag, ref ActorAttribute attribute)
    {
        bool contains = attributes.ContainsKey(tag);
        if (contains)
        {
            attribute = attributes[tag];
        }

        return contains;
    }

    public AbilityHandle AddAbility(Type abilityType)
    {
        ConstructorInfo constructor = abilityType.GetConstructor(Array.Empty<Type>());
        if (!typeof(Ability).IsAssignableFrom(abilityType) || constructor == null)
        {
            return AbilityHandle.Generate(null);
        }

        Ability ability = (Ability)constructor.Invoke(null);
        ability.SetUp(this);

        AbilityHandle handle = AbilityHandle.Generate(ability);
        abilities.Add(handle, ability);

        return handle;
    }

    public AbilityHandle AddAbility<T>() where T : Ability, new()
    {
        Ability ability = new T();
        ability.SetUp(this);
        AbilityHandle handle = AbilityHandle.Generate(ability);
        abilities.Add(handle, ability);

        return handle;
    }

    public void RemoveAbility(AbilityHandle handle)
    {
        if (Monitor.IsEntered(abilities))
        {
            if (!abilities.TryGetValue(handle, out Ability ability))
            {
                return;
            }

            ability.IsPendingRemove = true;
            if (abilityRemoveTask == null || abilityRemoveTask.IsCompleted)
            {
                abilityRemoveTask = WaitAbilityRemove();
            }
        }
        else
        {
            if (abilities.TryGetValue(handle, out Ability ability))
            {
                abilities.Remove(handle);
                ability.DisposeInternal();
            }
        }
    }

    public void RemoveAbility(Ability ability)
    {
        KeyValuePair<AbilityHandle, Ability> pair = abilities.FirstOrDefault((pair) => pair.Value == ability);
        RemoveAbility(pair.Key);
    }

    public void ActivateAbility(AbilityHandle handle)
    {
        if (!abilities.TryGetValue(handle, out Ability ability) || ability.IsPendingRemove)
        {
            return;
        }

        ability.ActivateInternal();
    }

    public void CancelAbility(AbilityHandle handle)
    {
        if (!abilities.ContainsKey(handle))
        {
            return;
        }

        abilities[handle].CancelInternal();
    }

    public void ActivateByAbilityEvent(AbilityEvent abilityEvent)
    {
        if (abilityEvent == null)
        {
            return;
        }

        lock (abilities)
        {
            foreach (KeyValuePair<AbilityHandle, Ability> pair in abilities)
            {
                Ability ability = pair.Value;
                if (ability.IsPendingRemove)
                {
                    continue;
                }

                ability.ProcessActivateAbilityEvent(abilityEvent);
            }
        }
    }

    public void CancelByAbilityEvent(AbilityEvent abilityEvent)
    {
        if (abilityEvent == null)
        {
            return;
        }

        lock (abilities)
        {
            foreach (KeyValuePair<AbilityHandle, Ability> pair in abilities)
            {
                Ability ability = pair.Value;
                if (ability.IsPendingRemove)
                {
                    continue;
                }

                ability.ProcessCancelAbilityEvent(abilityEvent);
            }
        }
    }

    public void ResetAbilities()
    {
        lock (abilities)
        {
            foreach (KeyValuePair<AbilityHandle, Ability> pair in abilities)
            {
                Ability ability = pair.Value;
                ability.CancelInternal();
            }
        }
    }

    public EffectHandle ApplyEffect(Type effectType, AbilityComponent instigator)
    {
        if (effectType == null || instigator == null || !typeof(ActorEffect).IsAssignableFrom(effectType))
        {
            return EffectHandle.Generate(null);
        }

        ConstructorInfo constructor = effectType.GetConstructor(Array.Empty<Type>());
        if (constructor == null)
        {
            return EffectHandle.Generate(null);
        }

        ActorEffect effect = (ActorEffect)constructor.Invoke(null);
        effect.Construct();
        return ApplyEffectInternal(effect, instigator);
    }

    public EffectHandle ApplyEffect<DATA_TYPE>(Type effectType, AbilityComponent instigator, DATA_TYPE data)
    {
        if (effectType == null || instigator == null || !typeof(ActorEffect<DATA_TYPE>).IsAssignableFrom(effectType))
        {
            return EffectHandle.Generate(null);
        }

        ConstructorInfo constructor = effectType.GetConstructor(Array.Empty<Type>());
        if (constructor == null)
        {
            return EffectHandle.Generate(null);
        }

        ActorEffect<DATA_TYPE> effect = (ActorEffect<DATA_TYPE>)constructor.Invoke(null);
        effect.ConstructWithData(data);
        return ApplyEffectInternal(effect, instigator);
    }

    public EffectHandle ApplyEffect<T>(AbilityComponent instigator) where T : ActorEffect, new()
    {
        if (instigator == null)
        {
            return EffectHandle.Generate(null);
        }

        ActorEffect effect = new T();
        effect.Construct();
        return ApplyEffectInternal(effect, instigator);
    }

    public EffectHandle ApplyEffect<T, DATA_TYPE>(AbilityComponent instigator, DATA_TYPE data)
        where T : ActorEffect<DATA_TYPE>, new()
    {
        if (instigator == null)
        {
            return EffectHandle.Generate(null);
        }

        ActorEffect<DATA_TYPE> effect = new T();
        effect.ConstructWithData(data);
        return ApplyEffectInternal(effect, instigator);
    }

    private EffectHandle ApplyEffectInternal(ActorEffect effect, AbilityComponent instigator)
    {
        effect.SetTarget(instigator, this);

        if (!effect.CanApply())
        {
            return EffectHandle.Generate(null);
        }

        PreApplyEffect(effect);

        lock (effects)
        {
            foreach (KeyValuePair<EffectHandle, ActorEffect> pair in effects.Where((pair) => effect.IsNeedRemoveOnApply(pair.Value)))
            {
                RemoveEffect(pair.Key);
            }

            ELifeTimePolicy durationPolicy = effect.DurationPolicy;

            GameplayTag[] attributeTags = attributes.Keys.ToArray();
            ReadOnlyCollection<GameplayTag> targetAttributeTags;
            if (durationPolicy != ELifeTimePolicy.Permanant && effect.AllowStack)
            {
                Type effectType = effect.GetType();
                KeyValuePair<EffectHandle, ActorEffect> pair = effects.FirstOrDefault((KeyValuePair<EffectHandle, ActorEffect> pair) => !pair.Value.IsPendingRemove && pair.Value.GetType().Equals(effectType));
                if (pair.Key.IsValid())
                {
                    if (!ProcessEffectStack(pair.Key, pair.Value, effect))
                    {
                        return EffectHandle.Generate(null);
                    }

                    IEnumerable<ActorAttribute> updated = RecalculateTargetAttributes(effect);
                    PostApplyEffectInternal(updated, effect);

                    return pair.Key;
                }
            }

            EffectHandle handle;
            if (durationPolicy == ELifeTimePolicy.Permanant)
            {
                handle = EffectHandle.Generate(null);
            }
            else
            {
                handle = EffectHandle.Generate(effect);
                effects.Add(handle, effect);
            }

            effect.InitInternal();

            List<ActorAttribute> targetAttributes = new List<ActorAttribute>();
            targetAttributeTags = effect.TargetAttributeTags;
            foreach (GameplayTag targetAttributeTag in targetAttributeTags)
            {
                if (!attributes.TryGetValue(targetAttributeTag, out ActorAttribute attribute))
                {
                    continue;
                }

                if (durationPolicy == ELifeTimePolicy.Permanant)
                {
                    float oldBaseValue = attribute.BaseValue;
                    float newBaseValue = effect.Modify(attribute.BaseValue, attribute.CurrentValue);
                    newBaseValue = ValidateAttribute(targetAttributeTag, oldBaseValue, newBaseValue);
                    attribute.BaseValue = newBaseValue;
                }
                else
                {
                    attribute.AddEffect(effect);
                }

                if (!targetAttributes.Contains(attribute))
                {
                    targetAttributes.Add(attribute);
                }
            }

            if (durationPolicy == ELifeTimePolicy.Temporary)
            {
                AddExpirationTimer(handle, effect);
            }

            PostApplyEffectInternal(targetAttributes, effect);

            return handle;
        }
    }

    private bool ProcessEffectStack(EffectHandle existHandle, ActorEffect existEffect, ActorEffect newEffect)
    {
        ELifeTimePolicy durationPolicy = newEffect.DurationPolicy;
        EStackDurationPolicy stackDurationPolicy = existEffect.StackDurationPolicy;
        EStackExpirationPolicy stackExpirationPolicy = existEffect.StackExpirationPolicy;

        existEffect.StackEffect(newEffect);

        if (stackDurationPolicy == EStackDurationPolicy.None)
        {
            return true;
        }

        

        if (durationPolicy == ELifeTimePolicy.Temporary)
        {
            if (stackDurationPolicy == EStackDurationPolicy.Individual)
            {
                AddExpirationTimer(existHandle, existEffect);
            }
            else
            {
                int timerIndex = effectDurationTimers.FindIndex((item) => item.effectHandle.Equals(existHandle));
                if (timerIndex < 0)
                {
                    return false;
                }

                TimerHandle timerHandle = effectDurationTimers[timerIndex].timerHandle;
                float duration = existEffect.Duration;
                float newEffectDuration = stackDurationPolicy == EStackDurationPolicy.Refresh ? duration : effectTimerManager.GetRemainTime(timerHandle) + duration;
                effectTimerManager.SetRemainTime(timerHandle, newEffectDuration);
            }
        }

        return true;
    }

    private void AddExpirationTimer(EffectHandle handle, ActorEffect effect)
    {
        effectDurationTimers.Add(new EffectTimerHandle()
        {
            effectHandle = handle,
            timerHandle = effectTimerManager.Start(() => ProcessEffectExpiration(handle), MathF.Max(effect.Duration, 0.1f))
        });
    }

    private void ProcessEffectExpiration(EffectHandle handle)
    {
        if (!effects.TryGetValue(handle, out ActorEffect effect))
        {
            return;
        }

        if (!effect.AllowStack)
        {
            RemoveEffect(handle);
        }
        else
        {
            switch (effect.StackExpirationPolicy)
            {
                case EStackExpirationPolicy.Clear:
                    RemoveEffect(handle);
                    break;

                case EStackExpirationPolicy.Refresh:
                    effect.Stack = effect.UpdateStackCount();
                    if (effect.Stack <= 0)
                    {
                        RemoveEffect(handle);
                        break;
                    }

                    int index = effectDurationTimers.FindIndex((effectTimerHandle) => effectTimerHandle.effectHandle.IsValid() && effectTimerHandle.effectHandle.Equals(handle));
                    if (index < 0)
                    {
                        break;
                    }

                    effectDurationTimers.RemoveAt(index);

                    AddExpirationTimer(handle, effect);

                    RecalculateTargetAttributes(effect);

                    break;
            }
        }
    }

    private IEnumerable<ActorAttribute> RecalculateTargetAttributes(ActorEffect effect)
    {
        List<ActorAttribute> affected = new List<ActorAttribute>();
        ReadOnlyCollection<GameplayTag> targetAttributeTags = effect.TargetAttributeTags;
        foreach (GameplayTag targetAttributeTag in targetAttributeTags)
        {
            if (!attributes.TryGetValue(targetAttributeTag, out ActorAttribute attribute))
            {
                continue;
            }

            attribute.UpdateValue();
            affected.Add(attribute);
        }

        return affected;
    }

    public void RemoveEffect(EffectHandle handle)
    {
        if (Monitor.IsEntered(effects))
        {
            if (!effects.TryGetValue(handle, out ActorEffect effect))
            {
                return;
            }

            effect.IsPendingRemove = true;
            if (effectRemoveTask == null || effectRemoveTask.IsCompleted)
            {
                effectRemoveTask = WaitEffectRemove();
            }
        }
        else
        {
            if (!effects.Remove(handle, out ActorEffect effect))
            {
                return;
            }

            effectDurationTimers.RemoveAll((item) => item.effectHandle.Equals(handle));

            PreRemoveEffect(effect);

            List<ActorAttribute> affectedAttributes = new List<ActorAttribute>();
            foreach (GameplayTag targetAttributeTag in effect.TargetAttributeTags)
            {
                if (!attributes.TryGetValue(targetAttributeTag, out ActorAttribute attribute))
                {
                    continue;
                }

                attribute.RemoveEffect(effect);
                affectedAttributes.Add(attribute);
            }

            effect.DisposeInternal();

            PostRemoveEffectInternal(affectedAttributes, effect);
        }
    }

    public void RemoveEffect<T>() where T : ActorEffect
    {
        RemoveEffect(typeof(T));
    }

    public void RemoveEffect(Type type)
    {
        if (!typeof(ActorEffect).IsAssignableFrom(type))
        {
            return;
        }

        KeyValuePair<EffectHandle, ActorEffect>[] pairs = effects.Where((pair) => type == pair.Value.GetType()).ToArray();
        foreach (KeyValuePair<EffectHandle, ActorEffect> pair in pairs)
        {
            RemoveEffect(pair.Key);
        }
    }

    public bool HasEffect<T>() where T : ActorEffect
    {
        return HasEffect(typeof(T));
    }

    public bool HasEffect(Type type)
    {
        if (typeof(ActorEffect).IsAssignableFrom(type))
        {
            return false;
        }

        return effects.Any((pair) => pair.Value.GetType().Equals(type));
    }

    private async Task WaitAbilityRemove()
    {
        await Task.Run(async () =>
        {
            while (Monitor.IsEntered(abilities))
            {
                await Task.Delay(WaitFrequency, removeTokenSource.Token);
            }
        }, removeTokenSource.Token);

        List<AbilityHandle> needRemoveHandles = abilities.Where((pair) => pair.Value.IsPendingRemove).Select((pair) => pair.Key).ToList();
        foreach (AbilityHandle handle in needRemoveHandles)
        {
            RemoveAbility(handle);
        }
    }

    private async Task WaitEffectRemove()
    {
        await Task.Run(async () =>
        {
            while (Monitor.IsEntered(effects))
            {
                await Task.Delay(WaitFrequency, removeTokenSource.Token);
            }
        }, removeTokenSource.Token);

        List<EffectHandle> needRemoveHandles = effects.Where((pair) => pair.Value.IsPendingRemove).Select(((pair) => pair.Key)).ToList();
        foreach (EffectHandle handle in needRemoveHandles)
        {
            RemoveEffect(handle);
        }
    }

    protected virtual float ValidateAttribute(GameplayTag tag, float oldBaseValue, float baseValue)
    {
        return baseValue;
    }

    protected virtual void PreApplyEffect(ActorEffect effect) { }

    private void PostApplyEffectInternal(IEnumerable<ActorAttribute> affected, ActorEffect effect)
    {
        List<GameplayTagContainer> effectTagContainers = new List<GameplayTagContainer>(effects.Select((pair) => pair.Value.TagContainer));
        tagContainer = new GameplayTagContainer(effectTagContainers);

        foreach (KeyValuePair<AbilityHandle, Ability> pair in abilities)
        {
            pair.Value.OnApplyEffect(affected, effect);
        }

        PostApplyEffect(affected, effect);
    }
    protected virtual void PostApplyEffect(IEnumerable<ActorAttribute> affected, ActorEffect effect) { }

    protected virtual void PreRemoveEffect(ActorEffect effect) { }

    private void PostRemoveEffectInternal(IEnumerable<ActorAttribute> affected, ActorEffect effect)
    {
        List<GameplayTagContainer> effectTagContainers = new List<GameplayTagContainer>(effects.Select((pair) => pair.Value.TagContainer));
        tagContainer = new GameplayTagContainer(effectTagContainers);

        foreach (KeyValuePair<AbilityHandle, Ability> pair in abilities)
        {
            pair.Value.OnRemoveEffect(affected, effect);
        }

        PostRemoveEffect(affected, effect);
    }
    protected virtual void PostRemoveEffect(IEnumerable<ActorAttribute> affected, ActorEffect effect) { }
}