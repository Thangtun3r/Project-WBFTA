using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModifierDatabaseFactory : MonoBehaviour
{
    public static ModifierDatabaseFactory Instance { get; private set; }

    [Serializable]
    public class ModifierEntry
    {
        public string ModifierID;
        public ModifierDefinition Definition;
        public string LogicClassName;
    }

    [SerializeField] private List<ModifierEntry> modifierEntries = new List<ModifierEntry>();

    private Dictionary<string, ModifierEntry> _entryLookup;
    private Dictionary<string, Type> _logicCache;

    private void Awake()
    {
        EnsureSingleton();
        InitializeCatalog();
    }

    private void EnsureSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeCatalog()
    {
        _entryLookup = new Dictionary<string, ModifierEntry>();
        foreach (var entry in modifierEntries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ModifierID))
            {
                Debug.LogWarning("ModifierDatabaseFactory: Skipping entry with missing ModifierID.");
                continue;
            }

            if (_entryLookup.ContainsKey(entry.ModifierID))
            {
                Debug.LogWarning($"ModifierDatabaseFactory: Duplicate ModifierID '{entry.ModifierID}' ignored.");
                continue;
            }

            _entryLookup.Add(entry.ModifierID, entry);
        }

        _logicCache = new Dictionary<string, Type>();
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                     .Where(t => typeof(IModifierLogic).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            if (_logicCache.ContainsKey(type.Name))
            {
                Debug.LogWarning($"ModifierDatabaseFactory: Duplicate modifier logic class name '{type.Name}' ignored.");
                continue;
            }

            _logicCache.Add(type.Name, type);
        }
    }

    public (ModifierDefinition definition, IModifierLogic logic) CreateModifier(string modifierId)
    {
        if (_entryLookup == null || !_entryLookup.TryGetValue(modifierId, out var entry))
        {
            return (null, null);
        }

        IModifierLogic logic = CreateLogicInstance(entry.LogicClassName);
        return (entry.Definition, logic);
    }

    public ModifierDefinition GetDefinition(string modifierId)
    {
        return _entryLookup != null && _entryLookup.TryGetValue(modifierId, out var entry)
            ? entry.Definition
            : null;
    }

    public IReadOnlyList<ModifierEntry> GetAllEntries()
    {
        return modifierEntries;
    }

    private IModifierLogic CreateLogicInstance(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        if (_logicCache != null && _logicCache.TryGetValue(className, out Type logicType))
        {
            return (IModifierLogic)Activator.CreateInstance(logicType);
        }

        return null;
    }
}
