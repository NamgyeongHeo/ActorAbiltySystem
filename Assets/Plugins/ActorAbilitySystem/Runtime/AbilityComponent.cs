using GameplayTags;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ActorAbilitySystem
{
    public interface IAbilityOwnable
    {
        AbilityComponent GetAbilityComponent();
    }

    public struct AbilityHandle
    {
        private static int maxIndex = 0;

        private int index;
        private WeakReference<ActorAbility> ability;
        public WeakReference<ActorAbility> Ref
        {
            get
            {
                return ability;
            }
        }

        internal static AbilityHandle Generate(ActorAbility ability)
        {
            if (ability == null)
            {
                return new AbilityHandle() { index = -1 };
            }

            return new AbilityHandle() { ability = new WeakReference<ActorAbility>(ability), index = maxIndex++ };
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

        private Dictionary<AbilityHandle, ActorAbility> abilities;
        private Dictionary<EffectHandle, TemporaryActorEffect> effects;
        private List<EffectTimerHandle> effectDurationTimers;

        private TimerManager timerManager;
        internal TimerManager TimerManager
        {
            get
            {
                return timerManager;
            }
        }

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

        public AbilityComponent(IAbilityOwnable owner, IEnumerable<AttributeInitializationInfo> attributeInitializers, ITimerHandler timerHandler = null)
        {
            removeTokenSource = new CancellationTokenSource();

            tagContainer = GameplayTagContainer.Create();
            timerManager = new TimerManager(timerHandler);

            attributes = new Dictionary<GameplayTag, ActorAttribute>();
            abilities = new Dictionary<AbilityHandle, ActorAbility>();
            effects = new Dictionary<EffectHandle, TemporaryActorEffect>();
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

            timerManager?.Dispose();
            timerManager = null;
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
            if (!typeof(ActorAbility).IsAssignableFrom(abilityType) || constructor == null)
            {
                return AbilityHandle.Generate(null);
            }

            ActorAbility ability = (ActorAbility)constructor.Invoke(null);
            ability.SetUp(this);

            AbilityHandle handle = AbilityHandle.Generate(ability);
            abilities.Add(handle, ability);

            return handle;
        }

        public AbilityHandle AddAbility<T>() where T : ActorAbility, new()
        {
            ActorAbility ability = new T();
            ability.SetUp(this);
            AbilityHandle handle = AbilityHandle.Generate(ability);
            abilities.Add(handle, ability);

            return handle;
        }

        public void RemoveAbility(AbilityHandle handle)
        {
            if (Monitor.IsEntered(abilities))
            {
                if (!abilities.TryGetValue(handle, out ActorAbility ability))
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
                if (abilities.TryGetValue(handle, out ActorAbility ability))
                {
                    abilities.Remove(handle);
                    ability.DisposeInternal();
                }
            }
        }

        public void RemoveAbility(ActorAbility ability)
        {
            KeyValuePair<AbilityHandle, ActorAbility> pair = abilities.FirstOrDefault((pair) => pair.Value == ability);
            RemoveAbility(pair.Key);
        }

        public void ActivateAbility(AbilityHandle handle)
        {
            if (!abilities.TryGetValue(handle, out ActorAbility ability) || ability.IsPendingRemove || ability.Disposed)
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
                foreach (KeyValuePair<AbilityHandle, ActorAbility> pair in abilities)
                {
                    ActorAbility ability = pair.Value;
                    if (ability.IsPendingRemove || ability.Disposed)
                    {
                        continue;
                    }

                    IEnumerable<Type> listenerTypes = ability.GetType().GetInterfaces()
                        .Where((interfaceType) =>
                        interfaceType.IsGenericType &&
                        interfaceType.GetGenericTypeDefinition() == typeof(IActivateAbilityEventListener<>) &&
                        abilityEvent.GetType().IsAssignableFrom(interfaceType.GenericTypeArguments[0]));

                    foreach (Type listenerType in listenerTypes)
                    {
                        InterfaceMapping mapping = ability.GetType().GetInterfaceMap(listenerType);
                        MethodInfo targetMethod = mapping.TargetMethods.FirstOrDefault((method) => method.Name == IActivateAbilityEventListener<AbilityEvent>.Activate_MethodName);
                        if (targetMethod == null)
                        {
                            continue;
                        }

                        targetMethod.Invoke(ability, new AbilityEvent[] { abilityEvent });
                    }

                    //ability.ProcessActivateAbilityEvent(abilityEvent);
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
                foreach (KeyValuePair<AbilityHandle, ActorAbility> pair in abilities)
                {
                    ActorAbility ability = pair.Value;
                    if (ability.IsPendingRemove || ability.Disposed)
                    {
                        continue;
                    }

                    IEnumerable<Type> listenerTypes = ability.GetType().GetInterfaces()
                        .Where((interfaceType) =>
                        interfaceType.IsGenericType &&
                        interfaceType.GetGenericTypeDefinition() == typeof(ICancelAbilityEventListener<>) &&
                        abilityEvent.GetType().IsAssignableFrom(interfaceType.GenericTypeArguments[0]));

                    foreach (Type listenerType in listenerTypes)
                    {
                        InterfaceMapping mapping = ability.GetType().GetInterfaceMap(listenerType);
                        MethodInfo targetMethod = mapping.TargetMethods.FirstOrDefault((method) => method.Name == ICancelAbilityEventListener<AbilityEvent>.Cancel_MethodName);
                        if (targetMethod == null)
                        {
                            continue;
                        }

                        targetMethod.Invoke(ability, new AbilityEvent[] { abilityEvent });
                    }

                    //ability.ProcessCancelAbilityEvent(abilityEvent);
                }
            }
        }

        public void ResetAbilities()
        {
            lock (abilities)
            {
                foreach (KeyValuePair<AbilityHandle, ActorAbility> pair in abilities)
                {
                    ActorAbility ability = pair.Value;
                    ability.CancelInternal();
                }
            }
        }

        public EffectHandle ApplyEffect(ActorEffect effect, AbilityComponent instigator)
        {
            if (effect.DidApply)
            {
                Debug.LogError("ActorEffect can't apply more than once.");
                return EffectHandle.Generate(null);
            }

            effect.SetTarget(instigator, this);

            if (!effect.CanApply())
            {
                return EffectHandle.Generate(null);
            }

            PreApplyEffect(effect);

            lock (effects)
            {
                foreach (KeyValuePair<EffectHandle, TemporaryActorEffect> pair in effects.Where((pair) => effect.IsNeedRemoveOnApply(pair.Value)))
                {
                    RemoveEffect(pair.Key);
                }

                GameplayTag[] attributeTags = attributes.Keys.ToArray();
                ReadOnlyCollection<GameplayTag> targetAttributeTags;
                TemporaryActorEffect tempEffect = effect as TemporaryActorEffect;
                if (tempEffect != null)
                {
                    Type effectType = effect.GetType();
                    KeyValuePair<EffectHandle, TemporaryActorEffect> pair = effects.FirstOrDefault((KeyValuePair<EffectHandle, TemporaryActorEffect> pair) => !pair.Value.IsPendingRemove && pair.Value.GetType().Equals(effectType));
                    if (pair.Key.IsValid())
                    {
                        pair.Value.ProcessStack(tempEffect);
                        IEnumerable<ActorAttribute> updated = RecalculateTargetAttributes(effect);
                        PostApplyEffectInternal(updated, effect);

                        return pair.Key;
                    }
                }

                EffectHandle handle;
                if (tempEffect != null)
                {
                    handle = EffectHandle.Generate(tempEffect);
                    effects.Add(handle, tempEffect);
                }
                else
                {
                    handle = EffectHandle.Generate(null);
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

                    if (tempEffect == null)
                    {
                        float oldBaseValue = attribute.BaseValue;
                        float newBaseValue = effect.Modify(attribute.BaseValue, attribute.CurrentValue);
                        newBaseValue = ValidateAttribute(targetAttributeTag, oldBaseValue, newBaseValue);
                        attribute.BaseValue = newBaseValue;
                    }
                    else
                    {
                        attribute.AddEffect(tempEffect);
                    }

                    if (!targetAttributes.Contains(attribute))
                    {
                        targetAttributes.Add(attribute);
                    }
                }

                PostApplyEffectInternal(targetAttributes, effect);

                return handle;
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
                if (!effects.TryGetValue(handle, out TemporaryActorEffect effect))
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
                if (!effects.Remove(handle, out TemporaryActorEffect effect))
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

            KeyValuePair<EffectHandle, TemporaryActorEffect>[] pairs = effects.Where((pair) => type == pair.Value.GetType()).ToArray();
            foreach (KeyValuePair<EffectHandle, TemporaryActorEffect> pair in pairs)
            {
                RemoveEffect(pair.Key);
            }
        }

        public void ExpireEffect(EffectHandle handle)
        {
            if (!effects.TryGetValue(handle, out TemporaryActorEffect effect))
            {
                return;
            }

            effect.Expire();
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

            List<AbilityHandle> needRemoveHandles = abilities
                .Where((pair) => pair.Value.IsPendingRemove || pair.Value.Disposed)
                .Select((pair) => pair.Key).ToList();
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

            foreach (KeyValuePair<AbilityHandle, ActorAbility> pair in abilities)
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

            foreach (KeyValuePair<AbilityHandle, ActorAbility> pair in abilities)
            {
                pair.Value.OnRemoveEffect(affected, effect);
            }

            PostRemoveEffect(affected, effect);
        }
        protected virtual void PostRemoveEffect(IEnumerable<ActorAttribute> affected, ActorEffect effect) { }
    }
}