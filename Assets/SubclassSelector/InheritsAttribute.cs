using System;
using UnityEngine;

public class InheritsAttribute : PropertyAttribute
{
    Type type;

    public Type Type
    {
        get 
        { 
            return type; 
        }
    }

    public InheritsAttribute(Type type)
    {
        this.type = type;
    }
}
