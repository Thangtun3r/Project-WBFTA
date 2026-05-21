using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
#if UNITY_EDITOR
    [SerializeField] private bool autoRegisterDefinitions = true;
    [SerializeField] private string[] assetSearchFolders = { "Assets/_Item System/Scriptable Objects" };
#endif

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
        _entryLookup = new Dictionary<string, ModifierEntry>(StringComparer.Ordinal);
        foreach (ModifierEntry entry in modifierEntries)
        {
            TryRegisterEntry(entry);
        }
    }

    public (ModifierDefinition definition, IModifierLogic logic) CreateModifier(string modifierId)
    {
        if (_entryLookup == null || !_entryLookup.TryGetValue(modifierId, out var entry))
        {
            return (null, null);
        }

        IModifierLogic logic = CreateLogicInstance(ResolveEntryLogicClassName(entry));
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

    private void TryRegisterEntry(ModifierEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("ModifierDatabaseFactory: Skipping null modifier entry.");
            return;
        }

        string modifierId = Normalize(entry.ModifierID);
        if (string.IsNullOrWhiteSpace(modifierId))
        {
            Debug.LogWarning("ModifierDatabaseFactory: Skipping entry with missing ModifierID.");
            return;
        }

        if (_entryLookup.ContainsKey(modifierId))
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Duplicate ModifierID '{modifierId}' ignored.");
            return;
        }

        if (entry.Definition == null)
        {
            Debug.LogWarning($"ModifierDatabaseFactory: ModifierID '{modifierId}' has no ModifierDefinition assigned.");
        }
        else
        {
            ValidateDefinitionFields(modifierId, entry.Definition);
        }

        string logicClassName = ResolveEntryLogicClassName(entry);
        if (TryResolveLogicType(modifierId, logicClassName, out Type logicType) && entry.Definition != null)
        {
            ValidateRequiredParameters(modifierId, entry.Definition, logicType);
        }

        _entryLookup.Add(modifierId, entry);
    }

    private void ValidateDefinitionFields(string modifierId, ModifierDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(definition.modifierId))
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Modifier '{modifierId}' definition is missing modifierId.");
        }

        if (string.IsNullOrWhiteSpace(definition.description))
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Modifier '{modifierId}' definition is missing description.");
        }
    }

    private bool TryResolveLogicType(string modifierId, string className, out Type logicType)
    {
        logicType = null;
        if (string.IsNullOrWhiteSpace(className))
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Modifier '{modifierId}' is missing logicClassName.");
            return false;
        }

        if (_logicCache == null || !_logicCache.TryGetValue(className, out logicType))
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Modifier '{modifierId}' logicClassName '{className}' does not resolve to an IModifierLogic.");
            return false;
        }

        return true;
    }

    private void ValidateRequiredParameters(string modifierId, ModifierDefinition definition, Type logicType)
    {
        IModifierParameterRequirements requirements;
        try
        {
            requirements = Activator.CreateInstance(logicType) as IModifierParameterRequirements;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"ModifierDatabaseFactory: Could not validate parameters for modifier '{modifierId}' because '{logicType.Name}' failed to instantiate. {exception.Message}");
            return;
        }

        if (requirements == null || requirements.RequiredParameterKeys == null)
        {
            return;
        }

        IReadOnlyList<string> requiredKeys = requirements.RequiredParameterKeys;
        for (int i = 0; i < requiredKeys.Count; i++)
        {
            string key = requiredKeys[i];
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!HasParameterKey(definition.parameters, key))
            {
                Debug.LogWarning($"ModifierDatabaseFactory: Modifier '{modifierId}' is missing required parameter '{key}'.");
            }
        }
    }

    private string ResolveEntryLogicClassName(ModifierEntry entry)
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

    private static bool HasParameterKey(IReadOnlyList<ItemParameterEntry> parameters, string key)
    {
        if (parameters == null)
        {
            return false;
        }

        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].key == key)
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, Type> BuildLogicCache()
    {
        Dictionary<string, Type> logicCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!typeof(IModifierLogic).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                {
                    continue;
                }

                if (logicCache.ContainsKey(type.Name))
                {
                    Debug.LogWarning($"ModifierDatabaseFactory: Duplicate modifier logic class name '{type.Name}' ignored.");
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

    [ContextMenu("Rebuild Modifier Entries From Assets")]
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
        List<ModifierEntry> discoveredEntries = new List<ModifierEntry>();
        HashSet<string> discoveredIds = new HashSet<string>(StringComparer.Ordinal);
        string[] assetPaths = AssetDatabase.FindAssets("t:ModifierDefinition", folders)
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (string path in assetPaths)
        {
            ModifierDefinition definition = AssetDatabase.LoadAssetAtPath<ModifierDefinition>(path);
            if (definition == null)
            {
                Debug.LogWarning($"ModifierDatabaseFactory: ModifierDefinition asset could not be loaded at '{path}'.");
                continue;
            }

            string modifierId = ResolveModifierId(definition);
            if (string.IsNullOrWhiteSpace(modifierId))
            {
                Debug.LogWarning($"ModifierDatabaseFactory: Skipping modifier definition with no id at '{path}'.");
                continue;
            }

            if (!discoveredIds.Add(modifierId))
            {
                Debug.LogWarning($"ModifierDatabaseFactory: Duplicate modifierId '{modifierId}' found at '{path}'. Later asset ignored.");
                continue;
            }

            ValidateDefinitionFields(modifierId, definition);
            if (TryResolveLogicType(modifierId, definition.logicClassName, out Type logicType))
            {
                ValidateRequiredParameters(modifierId, definition, logicType);
            }

            discoveredEntries.Add(new ModifierEntry
            {
                ModifierID = modifierId,
                Definition = definition,
                LogicClassName = Normalize(definition.logicClassName)
            });
        }

        List<ModifierEntry> sortedEntries = discoveredEntries
            .OrderBy(entry => entry.ModifierID, StringComparer.Ordinal)
            .ToList();

        if (EntriesMatch(modifierEntries, sortedEntries))
        {
            return false;
        }

        modifierEntries = sortedEntries;

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

    private static string ResolveModifierId(ModifierDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        return Normalize(definition.modifierId);
    }

    private static bool EntriesMatch(IReadOnlyList<ModifierEntry> currentEntries, IReadOnlyList<ModifierEntry> newEntries)
    {
        if (currentEntries == null || currentEntries.Count != newEntries.Count)
        {
            return false;
        }

        for (int i = 0; i < currentEntries.Count; i++)
        {
            ModifierEntry current = currentEntries[i];
            ModifierEntry next = newEntries[i];
            if (current == null || next == null)
            {
                if (current != next)
                {
                    return false;
                }

                continue;
            }

            if (!StringComparer.Ordinal.Equals(Normalize(current.ModifierID), Normalize(next.ModifierID)) ||
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
