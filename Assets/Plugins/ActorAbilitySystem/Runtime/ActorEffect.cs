using GameplayTags;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace ActorAbilitySystem
{
    public abstract partial class ActorEffect : IDisposable
    {
        internal event Action onUpdate;

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
                    if (!typeof(ActorAbility).IsAssignableFrom(grantAbilityType))
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

        internal virtual void Construct()
        {
        }

        internal virtual void InitInternal()
        {
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
}