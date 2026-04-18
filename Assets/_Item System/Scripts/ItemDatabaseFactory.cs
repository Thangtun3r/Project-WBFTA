using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        _entryLookup = itemEntries.ToDictionary(e => e.ItemID, e => e);
        _logicCache = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => typeof(IItemLogic).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public (ItemDefinition definition, IItemLogic logic) CreateItem(string itemId)
    {
        if (!_entryLookup.TryGetValue(itemId, out var entry)) return (null, null);

        IItemLogic logic = CreateLogicInstance(entry.LogicClassName);
        return (entry.Definition, logic);
    }

    private IItemLogic CreateLogicInstance(string className)
    {
        if (_logicCache.TryGetValue(className, out Type logicType))
        {
            return (IItemLogic)Activator.CreateInstance(logicType);
        }
        return null;
    }
}