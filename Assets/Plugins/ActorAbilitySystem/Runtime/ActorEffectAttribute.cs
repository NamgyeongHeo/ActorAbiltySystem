
using System;
using System.Collections.ObjectModel;

namespace ActorAbilitySystem
{
    public partial class ActorEffect
    {
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