using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("GameplayTags.Editor")]
[Serializable]
public struct GameplayTag
{
    [Serializable]
    internal class GameplayTagRef
    {
        private string tagName;
        internal GameplayTag Tag
        {
            get
            {
                GameplayTagsList tagsList = GameplayTagsList.Instance;
                return tagsList.FindGameplayTag(tagName);
            }
        }

        internal GameplayTagRef(GameplayTag tag)
        {
            tagName = tag.name;
            //this.tag = tag;
        }
    }

    internal const char NAME_SEPARATOR = '.';

    [SerializeField]
    private string name;

    public string Name
    {
        get
        {
            return name;
        }
    }

    // Use Ref class for cancel struct define cycle. 
    [SerializeField]
    private string parentName;
    public GameplayTag Parent
    {
        get
        {
            if (string.IsNullOrEmpty(parentName))
            {
                return new GameplayTag();
            }

            return GameplayTagsList.Instance.FindGameplayTag(parentName);
        }
    }

    internal GameplayTag(string name)
    {
        parentName = null;
        this.name = name;
    }

    internal GameplayTag(string parentName, string name)
    {
        this.parentName = parentName;

        this.name = name;
    }

    public static GameplayTag Create(string name)
    {
        GameplayTagsList tagsList = GameplayTagsList.Instance;
        if (tagsList != null)
        {
            return tagsList.TagsRef.Find((GameplayTag tag) => tag.Name == name);
        }

        return new GameplayTag();
    }

    public bool IsValid()
    {
        GameplayTagsList tagsList = GameplayTagsList.Instance;
        return !string.IsNullOrEmpty(name) && tagsList != null && tagsList.TagsRef.Contains(this);
    }

    public bool Match(GameplayTag other)
    {
        string[] names = SplitTagName(name);
        string[] otherNames = SplitTagName(other.name);

        if (names.Length < otherNames.Length)
        {
            return false;
        }

        for (int i = 0; i < otherNames.Length; i++)
        {
            if (names[i] != otherNames[i])
            {
                return false;
            }
        }

        return true;
    }

    public bool MatchExact(GameplayTag other)
    {
        return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(other.name)
            && name.Equals(other.name);
    }

    public bool MatchAny(GameplayTagContainer others)
    {
        foreach (GameplayTag tag in others.Tags)
        {
            if (Match(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool MatchAnyExact(GameplayTagContainer others)
    {
        foreach (GameplayTag tag in others.Tags)
        {
            if (MatchExact(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool MatchAll(GameplayTagContainer others)
    {
        foreach (GameplayTag tag in others.Tags)
        {
            if (!Match(tag))
            {
                return false;
            }
        }

        return true;
    }

    public bool MatchAllExact(GameplayTagContainer others)
    {
        foreach (GameplayTag tag in others.Tags)
        {
            if (!MatchExact(tag))
            {
                return false;
            }
        }

        return true;
    }

    internal static string[] SplitTagName(string name)
    {
        return name.Split(NAME_SEPARATOR);
    }

    public override bool Equals(object obj)
    {
        return obj is GameplayTag tag
            && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(tag.name)
            && tag.name.Equals(name);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}