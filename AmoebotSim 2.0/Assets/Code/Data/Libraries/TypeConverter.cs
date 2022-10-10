using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class TypeConverter
{
    
    public static object ConvertStringToObjectOfType(Type type, string text)
    {
        if (type == typeof(bool)) return bool.Parse(text);
        else if (type == typeof(int)) return int.Parse(text);
        else if (type == typeof(float)) return float.Parse(text);
        else if (type == typeof(string)) return text;
        else if (type.IsSubclassOf(typeof(Enum))) return Enum.Parse(type, text);
        else
        {
            Log.Error("TypeConverter: ConvertStringToObjectOfType: For text " + text + " and type " + type.ToString() + " there is not conversion method available. Returning text.");
            return text;
        }
    }

}
