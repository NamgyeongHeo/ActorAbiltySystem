using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("SubclassSelector.Editor")]
[Serializable]
public class SubclassSelector
{
    public event Action onTypeSelected;

    [HideInInspector]
    [SerializeField]
    string selectedTypePath;

    [HideInInspector]
    [SerializeField]
    string baseTypePath;

    public Type SelectedType
    {
        get
        {
            if (string.IsNullOrEmpty(selectedTypePath))
            {
                return null;
            }

            return Type.GetType(selectedTypePath);
        }
    }

    public Type BaseType
    {
        get
        {
            return Type.GetType(baseTypePath);
        }
    }
}