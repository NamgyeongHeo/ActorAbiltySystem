using GameplayTags;
using System;
using System.Collections.Generic;

namespace ActorAbilitySystem
{
    public sealed class ActorAttribute
    {
        [AttributeUsage(AttributeTargets.Field)]
        public class InitializeAttribute : Attribute
        {
            private string tagName;
            public string TagName
            {
                get
                {
                    return tagName;
                }
            }

            private float initialValue;
            public float InitialValue
            {
                get
                {
                    return initialValue;
                }
            }

            public InitializeAttribute(string tagName, float initialValue = 0f)
            {
                this.tagName = tagName;
                this.initialValue = initialValue;
            }
        }

        public delegate void AttributeUpdateDelegate(float baseValue, float oldValue, float currentValue);
        public event AttributeUpdateDelegate onAttributeUpdate;

        private float baseValue;
        public float BaseValue
        {
            get
            {
                return baseValue;
            }
            internal set
            {
                baseValue = value;
                UpdateValue();
            }
        }

        private float currentValue;
        public float CurrentValue
        {
            get
            {
                foreach (ActorEffect effect in effects)
                {
                    if (effect.IsDirty())
                    {
                        UpdateValue();
                        break;
                    }
                }

                return currentValue;
            }
        }

        private GameplayTag tag;
        public GameplayTag Tag
        {
            get
            {
                return tag;
            }
        }

        private List<TemporaryActorEffect> effects;

        private ActorAttribute()
        {
            effects = new List<TemporaryActorEffect>();
        }

        public static ActorAttribute Create(GameplayTag tag, float initialValue)
        {
            GameplayTagsList tagsList = GameplayTagsList.Instance;
            if (tagsList == null || !tagsList.Tags.Contains(tag))
            {
                return null;
            }

            ActorAttribute attr = new ActorAttribute();
            attr.BaseValue = initialValue;
            attr.tag = tag;

            return attr;
        }

        internal void AddEffect(TemporaryActorEffect effect)
        {
            if (effects.Contains(effect))
            {
                return;
            }

            effects.Add(effect);
            effect.onUpdate += UpdateValue;
            UpdateValue();
        }

        internal void AddEffects(IEnumerable<TemporaryActorEffect> effects)
        {
            foreach (TemporaryActorEffect effect in effects)
            {
                if (this.effects.Contains(effect))
                {
                    return;
                }

                this.effects.Add(effect);
                effect.onUpdate += UpdateValue;
            }

            UpdateValue();
        }

        internal bool RemoveEffect(TemporaryActorEffect effect)
        {
            bool result = effects.Remove(effect);
            if (result)
            {
                effect.onUpdate -= UpdateValue;
                UpdateValue();
            }

            return result;
        }

        internal bool RemoveEffects(IEnumerable<TemporaryActorEffect> effects)
        {
            bool result = false;
            foreach (TemporaryActorEffect effect in effects)
            {
                result |= this.effects.Remove(effect);
                effect.onUpdate -= UpdateValue;
            }

            if (result)
            {
                UpdateValue();
            }

            return result;
        }

        internal void UpdateValue()
        {
            float oldCurrentValue = currentValue;
            currentValue = baseValue;

            effects.Sort((a, b) => b.Priority - a.Priority);
            foreach (TemporaryActorEffect effect in effects)
            {
                if (effect.IsPendingRemove)
                {
                    continue;
                }

                currentValue = effect.Modify(baseValue, currentValue);
            }

            onAttributeUpdate?.Invoke(baseValue, oldCurrentValue, currentValue);
        }
    }
}