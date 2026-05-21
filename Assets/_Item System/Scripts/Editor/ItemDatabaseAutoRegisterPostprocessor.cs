#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ItemDatabaseAutoRegisterPostprocessor : AssetPostprocessor
{
    private static bool _refreshQueued;

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!ContainsCatalogAsset(importedAssets) &&
            !ContainsCatalogAsset(deletedAssets) &&
            !ContainsCatalogAsset(movedAssets) &&
            !ContainsCatalogAsset(movedFromAssetPaths))
        {
            return;
        }

        QueueRefresh();
    }

    private static bool ContainsCatalogAsset(string[] paths)
    {
        if (paths == null)
        {
            return false;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
            {
                continue;
            }

            if (path.Contains("/Scriptable Objects/Items/") ||
                path.Contains("/Scriptable Objects/Modifiers/"))
            {
                return true;
            }
        }

        return false;
    }

    private static void QueueRefresh()
    {
        if (_refreshQueued)
        {
            return;
        }

        _refreshQueued = true;
        EditorApplication.delayCall += RefreshOpenDatabaseFactories;
    }

    private static void RefreshOpenDatabaseFactories()
    {
        _refreshQueued = false;

        foreach (ItemDatabaseFactory factory in Resources.FindObjectsOfTypeAll<ItemDatabaseFactory>())
        {
            if (IsPersistentAsset(factory))
            {
                continue;
            }

            if (factory.RebuildEntriesFromAssets())
            {
                MarkSceneDirty(factory.gameObject);
            }
        }

        foreach (ModifierDatabaseFactory factory in Resources.FindObjectsOfTypeAll<ModifierDatabaseFactory>())
        {
            if (IsPersistentAsset(factory))
            {
                continue;
            }

            if (factory.RebuildEntriesFromAssets())
            {
                MarkSceneDirty(factory.gameObject);
            }
        }
    }

    private static bool IsPersistentAsset(Object target)
    {
        return target == null || EditorUtility.IsPersistent(target);
    }

    private static void MarkSceneDirty(GameObject gameObject)
    {
        if (gameObject == null || !gameObject.scene.IsValid())
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
}
#endif
