#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ManagedReferenceUtility
{
    /// Creates instance of passed type and assigns it to managed reference
    public static object AssignNewInstanceOfTypeToManagedReference(this SerializedProperty serializedProperty, Type type)
    {
        object instance = Activator.CreateInstance(type, true);
        
        serializedProperty.serializedObject.Update(); 
        serializedProperty.managedReferenceValue = instance;
        serializedProperty.serializedObject.ApplyModifiedProperties(); 
        
        return instance;
    }

    /// Sets managed reference to null
    public static void SetManagedReferenceToNull(this SerializedProperty serializedProperty)
    {
        serializedProperty.serializedObject.Update();
        serializedProperty.managedReferenceValue = null;
        serializedProperty.serializedObject.ApplyModifiedProperties(); 
    }

    /// Collects appropriate types based on managed reference field type and filters. Filters all derive
    public static IEnumerable<Type> GetAppropriateTypesForAssigningToManagedReference(this SerializedProperty property, List<Func<Type, bool>> filters = null)
    {
        var fieldType = property.GetManagedReferenceFieldType();
        return GetAppropriateTypesForAssigningToManagedReference(fieldType, filters);
    }

    /// Filters derived types of field typ parameter and finds ones whose are compatible with managed reference and filters.
    public static IEnumerable<Type> GetAppropriateTypesForAssigningToManagedReference(Type fieldType, List<Func<Type, bool>> filters = null)
    {
        var appropriateTypes = new List<Type>();

        // New change: also include the base type if it is valid.
        if (ShouldTypeBeSelectable(fieldType, filters))
            appropriateTypes.Add(fieldType);

        // Get and filter all appropriate types
        var derivedTypes = TypeCache.GetTypesDerivedFrom(fieldType);
        foreach (var type in derivedTypes)
        {
            if (ShouldTypeBeSelectable(type, filters))
                appropriateTypes.Add(type);
        }

        return appropriateTypes;
    }

    private static bool ShouldTypeBeSelectable(Type type, List<Func<Type, bool>> filters)
    {
        // Skip interfaces since they cannot be instantiated.
        if (type.IsInterface)
            return false;

        // Skips unity engine Objects (because they are not serialized by SerializeReference)
        if (type.IsSubclassOf(typeof(Object)))
            return false;

        // Skip abstract classes because they should not be instantiated
        if (type.IsAbstract)
            return false;

        // Skip generic classes because they can not be instantiated
        if (type.ContainsGenericParameters)
            return false;

        // Skip classes that have no parameterless constructor, structs are always fine since they always have parameterless constructor.
        if (type.IsClass)
        {
            bool hasParameterlessConstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
            if (!hasParameterlessConstructor)
                return false;
        }

        // Filter types by provided filters if there is ones
        if (filters != null && filters.All(f => f == null || f.Invoke(type)) == false) 
            return false;

        return true;
    }
    
    /// Gets real type of managed reference
    public static Type GetManagedReferenceFieldType(this SerializedProperty property)
    {
        var realPropertyType = GetRealTypeFromTypename(property.managedReferenceFieldTypename);
        if (realPropertyType != null) 
            return realPropertyType;
        
        Debug.LogError($"Can not get field type of managed reference : {property.managedReferenceFieldTypename}");
        return null;
    }
    
    /// Gets real type of managed reference's field typeName
    public static Type GetRealTypeFromTypename(string stringType)
    {
        var names = GetSplitNamesFromTypename(stringType);
        var realType = Type.GetType($"{names.ClassName}, {names.AssemblyName}");
        return realType;
    }
    
    /// Get assembly and class names from typeName
    public static (string AssemblyName, string ClassName) GetSplitNamesFromTypename(string typename)
    {
        if (string.IsNullOrEmpty(typename))  
            return ("","");
        
        var typeSplitString = typename.Split(char.Parse(" "));
        var typeClassName = typeSplitString[1];
        var typeAssemblyName = typeSplitString[0];
        return (typeAssemblyName,  typeClassName); 
    }
}
#endif