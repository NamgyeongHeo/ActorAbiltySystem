using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

[Serializable]
public class GameplayTagContainer
{
    [SerializeField]
    internal List<GameplayTag> tags;

    public ReadOnlyCollection<GameplayTag> Tags
    {
        get
        {
            return tags.AsReadOnly();
        }
    }

    internal GameplayTagContainer()
    {
        tags = new List<GameplayTag>();
    }

    public GameplayTagContainer(GameplayTagContainer other)
    {
        if (other != null)
        {
            tags = new List<GameplayTag>(other.tags);
        }
        else
        {
            tags = new List<GameplayTag>();
        }
    }

    public GameplayTagContainer(IEnumerable<GameplayTagContainer> others)
    {
        tags = new List<GameplayTag>();

        foreach (GameplayTagContainer other in others)
        {
            foreach (GameplayTag otherTag in other.tags)
            {
                if (tags.Any((GameplayTag item) => item.Match(otherTag)))
                {
                    continue;
                }

                IEnumerable<GameplayTag> removes = tags.Where((GameplayTag item) => otherTag.Match(item));
                foreach (GameplayTag remove in removes)
                {
                    tags.Remove(remove);
                }

                tags.Add(otherTag);
            }
        }
    }

    public static GameplayTagContainer Create(params string[] names)
    {
        GameplayTagsList tagsList = GameplayTagsList.Instance;
        if (tagsList == null)
        {
            return null;
        }

        GameplayTagContainer tagContainer = new GameplayTagContainer();
        foreach (string name in names)
        {
            GameplayTag tag = tagsList.FindGameplayTag(name);
            if (tag.IsValid())
            {
                tagContainer.AddTag(tag);
            }
        }

        return tagContainer;
    }

    public void AddTag(GameplayTag tag)
    {
        if (!HasTag(tag))
        {
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag item = tags[i];
                if (tag.Match(item))
                {
                    if (tags.Remove(item))
                    {
                        i--;
                    }
                }
            }

            tags.Add(tag);
        }
    }

    public void Append(GameplayTagContainer tagContainer)
    {
        foreach (GameplayTag tag in tagContainer.tags)
        {
            AddTag(tag);
        }
    }

    public bool RemoveTag(GameplayTag tag)
    {
        GameplayTag[] matches = tags.Where((GameplayTag item) => item.Match(tag)).ToArray();
        if (matches.Length == 0)
        {
            return false;
        }

        foreach (GameplayTag match in matches)
        {
            tags.Remove(match);
        }

        if (tag.Parent.IsValid())
        {
            tags.Add(tag.Parent);
        }

        return true;
    }

    public void RemoveTags(GameplayTagContainer tagContainer)
    {
        foreach (GameplayTag tag in tagContainer.tags)
        {
            RemoveTag(tag);
        }
    }

    public bool HasTag(GameplayTag tag)
    {
        foreach (GameplayTag item in tags)
        {
            if (item.Match(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasTagExact(GameplayTag tag)
    {
        foreach (GameplayTag item in tags)
        {
            if (item.MatchExact(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAny(GameplayTagContainer other)
    {
        foreach (GameplayTag tag in other.tags)
        {
            if (HasTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAnyExact(GameplayTagContainer other)
    {
        foreach (GameplayTag tag in other.tags)
        {
            if (HasTagExact(tag))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAll(GameplayTagContainer other)
    {
        foreach (GameplayTag tag in other.tags)
        {
            if (!HasTag(tag))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasAllExact(GameplayTagContainer other)
    {
        foreach (GameplayTag tag in other.tags)
        {
            if (!HasTagExact(tag))
            {
                return false;
            }
        }

        return true;
    }
}