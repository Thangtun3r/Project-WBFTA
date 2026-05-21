using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
#if UNITY_EDITOR
    [SerializeField] private bool autoRegisterDefinitions = true;
    [SerializeField] private string[] assetSearchFolders = { "Assets/_Item System/Scriptable Objects" };
#endif
    
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
#if UNITY_EDITOR
        if (autoRegisterDefinitions)
        {
            RebuildEntriesFromAssets(false);
        }
#endif

        if (_logicCache == null)
        {
            _logicCache = BuildLogicCache();
        }
        _entryLookup = new Dictionary<string, ItemEntry>(StringComparer.Ordinal);
        foreach (ItemEntry entry in itemEntries)
        {
            TryRegisterEntry(entry);
        }
    }

    public (ItemDefinition definition, IItemLogic logic) CreateItem(string itemId)
    {
        if (!_entryLookup.TryGetValue(itemId, out var entry)) return (null, null);

        IItemLogic logic = CreateLogicInstance(ResolveEntryLogicClassName(entry));
        return (entry.Definition, logic);
    }

    public ItemDefinition GetDefinition(string itemId)
    {
        return _entryLookup.TryGetValue(itemId, out var entry) ? entry.Definition : null;
    }

    public IReadOnlyList<ItemEntry> GetAllEntries()
    {
        return itemEntries;
    }

    public string GetRandomItemId()
    {
        return GetRandomItemId(null);
    }

    public string GetRandomItemId(PlayerInventory inventory)
    {
        if (_entryLookup == null || _entryLookup.Count == 0)
        {
            Debug.LogWarning("ItemDatabaseFactory: No items available in the database.");
            return null;
        }

        if (inventory == null || inventory.ItemContext == null)
        {
            var itemIds = _entryLookup.Keys;
            int randomIndex = UnityEngine.Random.Range(0, itemIds.Count);
            return System.Linq.Enumerable.ElementAt(itemIds, randomIndex);
        }

        float totalWeight = 0f;
        List<(string itemId, float weight)> weightedItems = new List<(string itemId, float weight)>();

        foreach (KeyValuePair<string, ItemEntry> pair in _entryLookup)
        {
            float weight = inventory.ItemContext.CalculateItemDropWeight(pair.Key, pair.Value.Definition, 1f);
            if (weight <= 0f)
            {
                continue;
            }

            totalWeight += weight;
            weightedItems.Add((pair.Key, weight));
        }

        if (weightedItems.Count == 0 || totalWeight <= 0f)
        {
            Debug.LogWarning("ItemDatabaseFactory: No item drop weights were above zero.");
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < weightedItems.Count; i++)
        {
            roll -= weightedItems[i].weight;
            if (roll <= 0f)
            {
                return weightedItems[i].itemId;
            }
        }

        return weightedItems[weightedItems.Count - 1].itemId;
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

    private void TryRegisterEntry(ItemEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("ItemDatabaseFactory: Skipping null item entry.");
            return;
        }

        string itemId = Normalize(entry.ItemID);
        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning("ItemDatabaseFactory: Skipping entry with missing ItemID.");
            return;
        }

        if (_entryLookup.ContainsKey(itemId))
        {
            Debug.LogWarning($"ItemDatabaseFactory: Duplicate ItemID '{itemId}' ignored.");
            return;
        }

        if (entry.Definition == null)
        {
            Debug.LogWarning($"ItemDatabaseFactory: ItemID '{itemId}' has no ItemDefinition assigned.");
        }
        else
        {
            ValidateDefinitionFields(itemId, entry.Definition);
        }

        ValidateLogicClassName(itemId, ResolveEntryLogicClassName(entry));
        _entryLookup.Add(itemId, entry);
    }

    private void ValidateDefinitionFields(string itemId, ItemDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(definition.itemId))
        {
            Debug.LogWarning($"ItemDatabaseFactory: Item '{itemId}' definition is missing itemId.");
        }

        if (string.IsNullOrWhiteSpace(definition.description))
        {
            Debug.LogWarning($"ItemDatabaseFactory: Item '{itemId}' definition is missing description.");
        }
    }

    private void ValidateLogicClassName(string itemId, string className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            Debug.LogWarning($"ItemDatabaseFactory: Item '{itemId}' is missing logicClassName.");
            return;
        }

        if (_logicCache == null || !_logicCache.ContainsKey(className))
        {
            Debug.LogWarning($"ItemDatabaseFactory: Item '{itemId}' logicClassName '{className}' does not resolve to an IItemLogic.");
        }
    }

    private string ResolveEntryLogicClassName(ItemEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(entry.LogicClassName))
        {
            return entry.LogicClassName.Trim();
        }

        return entry.Definition != null ? Normalize(entry.Definition.logicClassName) : string.Empty;
    }

    private static Dictionary<string, Type> BuildLogicCache()
    {
        Dictionary<string, Type> logicCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!typeof(IItemLogic).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                {
                    continue;
                }

                if (logicCache.ContainsKey(type.Name))
                {
                    Debug.LogWarning($"ItemDatabaseFactory: Duplicate logic class name '{type.Name}' ignored.");
                    continue;
                }

                logicCache.Add(type.Name, type);
            }
        }

        return logicCache;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null);
        }
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoRegisterDefinitions)
        {
            RebuildEntriesFromAssets(false);
        }
    }

    [ContextMenu("Rebuild Item Entries From Assets")]
    private void RebuildEntriesFromAssetsContextMenu()
    {
        RebuildEntriesFromAssets(true);
    }

    public bool RebuildEntriesFromAssets()
    {
        return RebuildEntriesFromAssets(true);
    }

    private bool RebuildEntriesFromAssets(bool markDirty)
    {
        string[] folders = assetSearchFolders != null && assetSearchFolders.Length > 0
            ? assetSearchFolders
            : new[] { "Assets" };

        _logicCache = BuildLogicCache();
        List<ItemEntry> discoveredEntries = new List<ItemEntry>();
        HashSet<string> discoveredIds = new HashSet<string>(StringComparer.Ordinal);
        string[] assetPaths = AssetDatabase.FindAssets("t:ItemDefinition", folders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (string path in assetPaths)
        {
            ItemDefinition definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (definition == null)
            {
                Debug.LogWarning($"ItemDatabaseFactory: ItemDefinition asset could not be loaded at '{path}'.");
                continue;
            }

            string itemId = ResolveItemId(definition);
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"ItemDatabaseFactory: Skipping item definition with no id at '{path}'.");
                continue;
            }

            if (!discoveredIds.Add(itemId))
            {
                Debug.LogWarning($"ItemDatabaseFactory: Duplicate itemId '{itemId}' found at '{path}'. Later asset ignored.");
                continue;
            }

            ValidateDefinitionFields(itemId, definition);
            ValidateLogicClassName(itemId, definition.logicClassName);

            discoveredEntries.Add(new ItemEntry
            {
                ItemID = itemId,
                Definition = definition,
                LogicClassName = Normalize(definition.logicClassName)
            });
        }

        List<ItemEntry> sortedEntries = discoveredEntries
            .OrderBy(entry => entry.ItemID, StringComparer.Ordinal)
            .ToList();

        if (EntriesMatch(itemEntries, sortedEntries))
        {
            return false;
        }

        itemEntries = sortedEntries;

        if (markDirty)
        {
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }

        return true;
    }

    private static string ResolveItemId(ItemDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        return Normalize(definition.itemId);
    }

    private static bool EntriesMatch(IReadOnlyList<ItemEntry> currentEntries, IReadOnlyList<ItemEntry> newEntries)
    {
        if (currentEntries == null || currentEntries.Count != newEntries.Count)
        {
            return false;
        }

        for (int i = 0; i < currentEntries.Count; i++)
        {
            ItemEntry current = currentEntries[i];
            ItemEntry next = newEntries[i];
            if (current == null || next == null)
            {
                if (current != next)
                {
                    return false;
                }

                continue;
            }

            if (!StringComparer.Ordinal.Equals(Normalize(current.ItemID), Normalize(next.ItemID)) ||
                current.Definition != next.Definition ||
                !StringComparer.Ordinal.Equals(Normalize(current.LogicClassName), Normalize(next.LogicClassName)))
            {
                return false;
            }
        }

        return true;
    }
#endif
}
