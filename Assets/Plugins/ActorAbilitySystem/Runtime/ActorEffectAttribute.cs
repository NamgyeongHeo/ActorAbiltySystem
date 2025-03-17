
using System;
using System.Collections.ObjectModel;

namespace ActorAbilitySystem
{
    public partial class ActorEffect
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class StackPolicyAttribute : Attribute
        {
            private readonly EStackDurationPolicy durationPolicy;
            public EStackDurationPolicy DurationPolicy
            {
                get
                {
                    return durationPolicy;
                }
            }

            private readonly EStackExpirationPolicy expirationPolicy;
            public EStackExpirationPolicy ExpirationPolicy
            {
                get
                {
                    return expirationPolicy;
                }
            }

            public StackPolicyAttribute(EStackDurationPolicy durationPolicy = EStackDurationPolicy.None, EStackExpirationPolicy expirationPolicy = EStackExpirationPolicy.Clear)
            {
                this.durationPolicy = durationPolicy;
                this.expirationPolicy = expirationPolicy;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public class LifeTimePolicyAttribute : Attribute
        {
            private readonly ELifeTimePolicy policy;
            public ELifeTimePolicy Policy
            {
                get
                {
                    return policy;
                }
            }

            private readonly float duration;
            public float Duration
            {
                get
                {
                    return duration;
                }
            }

            public LifeTimePolicyAttribute(ELifeTimePolicy policy, float duration = 0f)
            {
                this.policy = policy;
                this.duration = duration;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
        public class GameplayTagsAttribute : Attribute
        {
            private string[] tagNames;
            internal ReadOnlyCollection<string> TagNames
            {
                get
                {
                    return new ReadOnlyCollection<string>(tagNames);
                }
            }

            public GameplayTagsAttribute(params string[] tagNames)
            {
                this.tagNames = tagNames;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
        public class TargetActorAttributeAttribute : Attribute
        {
            private string[] tagNames;
            public ReadOnlyCollection<string> TagNames
            {
                get
                {
                    return new ReadOnlyCollection<string>(tagNames);
                }
            }

            public TargetActorAttributeAttribute(params string[] tagNames)
            {
                this.tagNames = tagNames;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
        public class GrantAbilityAttribute : Attribute
        {
            private Type[] abilityTypes;
            public ReadOnlyCollection<Type> AbilityTypes
            {
                get
                {
                    return new ReadOnlyCollection<Type>(abilityTypes);
                }
            }

            public GrantAbilityAttribute(params Type[] abilityTypes)
            {
                this.abilityTypes = abilityTypes;
            }
        }
    }
}