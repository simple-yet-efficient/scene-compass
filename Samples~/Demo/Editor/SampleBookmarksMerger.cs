using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using SyE.SceneCompass.Editor;

/// <summary>
/// A utility script that automatically imports sample bookmarks when the editor loads.
/// Merges sample bookmarks with existing bookmarks if available.
/// </summary>
[InitializeOnLoad]
public class SampleBookmarksMerger
{
    private const string SampleDataPath = "Packages/com.sye.scenecompass/Samples~/sample-bookmarks.json";
    private const string UserDataPath = "Packages/com.sye.scenecompass/Editor/Resources/BookmarkData/bookmarks.json";
    private const string ImportedFlagKey = "SyE.SceneCompass.SampleBookmarksImported";

    // Static constructor that is called when Unity compiles the script
    static SampleBookmarksMerger()
    {
        // Run after a delay to ensure all assemblies are loaded
        EditorApplication.delayCall += () =>
        {
            // Check if samples are imported
            if (File.Exists(SampleDataPath))
            {
                ImportSampleBookmarks();
            }
        };
    }

    private static bool ImportSampleBookmarks()
    {
        try
        {
            // Load sample bookmarks
            if (!File.Exists(SampleDataPath))
            {
                Debug.LogWarning("Scene Compass: Sample bookmarks file not found at: " + SampleDataPath);
                return false;
            }

            string sampleJson = File.ReadAllText(SampleDataPath);
            var sampleData = JsonUtility.FromJson<BookmarkData>(sampleJson);

            if (sampleData == null || sampleData.bookmarks == null || sampleData.bookmarks.Count == 0)
            {
                Debug.LogWarning("Scene Compass: Sample bookmarks file contains no bookmarks.");
                return false;
            }

            // Load existing bookmarks (if any)
            List<BookmarkEntry> existingBookmarks = new List<BookmarkEntry>();
            Dictionary<string, bool> expansionStates = new Dictionary<string, bool>();
            string lastUsedGroup = "Default";
            
            if (File.Exists(UserDataPath))
            {
                try
                {
                    string userJson = File.ReadAllText(UserDataPath);
                    var userData = JsonUtility.FromJson<BookmarkData>(userJson);
                    
                    if (userData != null)
                    {
                        existingBookmarks = userData.bookmarks ?? new List<BookmarkEntry>();
                        lastUsedGroup = userData.lastUsedGroup ?? "Default";
                        
                        // Convert serialized expansion states to dictionary
                        if (userData.groupExpansionStates != null)
                        {
                            foreach (var state in userData.groupExpansionStates)
                            {
                                expansionStates[state.groupName] = state.isExpanded;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Scene Compass: Failed to load existing bookmarks: {e.Message}");
                    return false;
                }
            }

            // Add custom "Sample" group to all imported bookmarks
            foreach (var bookmark in sampleData.bookmarks)
            {
                if (!bookmark.group.StartsWith("Sample: "))
                {
                    bookmark.group = "Sample: " + bookmark.group;
                }
                
                // Check for duplicates (by name and group)
                bool isDuplicate = existingBookmarks.Exists(b => 
                    b.name == bookmark.name && 
                    b.group == bookmark.group);
                
                if (!isDuplicate)
                {
                    existingBookmarks.Add(bookmark);
                }
            }

            // Add sample groups to expansion states
            foreach (var bookmark in sampleData.bookmarks)
            {
                if (!expansionStates.ContainsKey(bookmark.group))
                {
                    expansionStates[bookmark.group] = true; // Expand sample groups by default
                }
            }

            // Create serializable expansion states
            var serializedStates = new List<GroupExpansionState>();
            foreach (var kvp in expansionStates)
            {
                serializedStates.Add(new GroupExpansionState 
                { 
                    groupName = kvp.Key, 
                    isExpanded = kvp.Value 
                });
            }

            // Save merged bookmarks
            var mergedData = new BookmarkData 
            { 
                bookmarks = existingBookmarks,
                lastUsedGroup = lastUsedGroup,
                groupExpansionStates = serializedStates
            };
            
            string json = JsonUtility.ToJson(mergedData, true);
            Directory.CreateDirectory(Path.GetDirectoryName(UserDataPath));
            File.WriteAllText(UserDataPath, json);
            
            // Log success
            Debug.Log($"Scene Compass: Successfully imported {sampleData.bookmarks.Count} sample bookmarks.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Scene Compass: Failed to import sample bookmarks: {e.Message}");
            return false;
        }
    }

    // Helper serializable classes that mirror the ones in BookmarkTool.cs
    [System.Serializable]
    private class BookmarkData
    {
        public List<BookmarkEntry> bookmarks;
        public string lastUsedGroup;
        public List<GroupExpansionState> groupExpansionStates = new List<GroupExpansionState>();
    }

    [System.Serializable]
    private class GroupExpansionState
    {
        public string groupName;
        public bool isExpanded;
    }
} 