using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemDatabaseFactory : MonoBehaviour
{
    public static ItemDatabaseFactory Instance { get; private set; }

    [Serializable]
    public class ItemEntry
    {
        public string ItemID;
        public ItemDefinition Definition;
        public string LogicClassName;
    }

    [SerializeField] private List<ItemEntry> itemEntries = new List<ItemEntry>();
    
    private Dictionary<string, ItemEntry> _entryLookup;
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
        _entryLookup = new Dictionary<string, ItemEntry>();
        foreach (var entry in itemEntries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemID))
            {
                Debug.LogWarning("ItemDatabaseFactory: Skipping entry with missing ItemID.");
                continue;
            }

            if (_entryLookup.ContainsKey(entry.ItemID))
            {
                Debug.LogWarning($"ItemDatabaseFactory: Duplicate ItemID '{entry.ItemID}' ignored.");
                continue;
            }

            _entryLookup.Add(entry.ItemID, entry);
        }

        _logicCache = new Dictionary<string, Type>();
        foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                     .Where(t => typeof(IItemLogic).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            if (_logicCache.ContainsKey(type.Name))
            {
                Debug.LogWarning($"ItemDatabaseFactory: Duplicate logic class name '{type.Name}' ignored.");
                continue;
            }

            _logicCache.Add(type.Name, type);
        }
    }

    public (ItemDefinition definition, IItemLogic logic) CreateItem(string itemId)
    {
        if (!_entryLookup.TryGetValue(itemId, out var entry)) return (null, null);

        IItemLogic logic = CreateLogicInstance(entry.LogicClassName);
        return (entry.Definition, logic);
    }

    public ItemDefinition GetDefinition(string itemId)
    {
        return _entryLookup.TryGetValue(itemId, out var entry) ? entry.Definition : null;
    }

    private IItemLogic CreateLogicInstance(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        if (_logicCache.TryGetValue(className, out Type logicType))
        {
            return (IItemLogic)Activator.CreateInstance(logicType);
        }
        return null;
    }
}