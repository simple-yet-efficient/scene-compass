using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using UnityEditor.ShortcutManagement;
using System.Linq;
using System.Diagnostics; // Required for Process.Start

namespace SyE.SceneCompass.Editor
{
    // Make this class public so the dialog can reference it
    [System.Serializable]
    public class BookmarkEntry
    {
        public BookmarkType type;
        public string name;
        public string group = "Default";
        public string iconPath; // Not used in current UI, but kept for data structure
        public string objectGlobalID;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public string sceneGUID;
    }

    public enum BookmarkType
    {
        Camera,
        GameObject
    }

public class BookmarkTool : EditorWindow
{
    private List<BookmarkEntry> bookmarks = new List<BookmarkEntry>();
    private string dataPath;
        private bool showCurrentSceneOnly = true;
        private string lastUsedGroup = "Default";
        private List<string> availableGroups = new List<string> { "Default" };
        private Dictionary<string, bool> groupExpansionStates = new Dictionary<string, bool>();
        private string searchQuery = "";
        private TextField searchField;

        private BookmarkEntry selectedBookmark = null;

        // UIElements references
        private VisualElement rootElement;
        private ScrollView scrollView;
        private VisualElement bookmarkListContainer; // Container for bookmark entries
        private Button addBookmarkButton;
        private Toggle currentSceneToggle;
        private Dictionary<string, VisualElement> groupContainers = new Dictionary<string, VisualElement>();

        private Texture2D bannerImage;
        private Texture2D syeLogoTexture;

        // Store window instance for direct access
        public static BookmarkTool instance;

        // Register menu item for opening window
        [MenuItem("Window/Scene Compass/Open Bookmarks %b", false, 0)]
    public static void ShowWindow()
    {
            instance = GetWindow<BookmarkTool>("Scene Compass - Bookmarks");
            instance.minSize = new Vector2(400, 300);
            instance.Show();
        }

        // Register menu item for hierarchy context menu
        [MenuItem("GameObject/Bookmark Object", false, 0)]
        static void BookmarkObjectMenuItem()
        {
            if (Selection.activeGameObject != null)
            {
                if (instance == null)
                    instance = GetWindow<BookmarkTool>("Scene Compass - Bookmarks", false);
                
                instance.Focus();
                instance.ShowAddBookmarkDialog();
            }
        }

        // Register the shortcut for opening bookmarks window
        [Shortcut("Scene Compass/Open Bookmarks Window", KeyCode.B, ShortcutModifiers.Alt)]
        public static void OpenBookmarksWindowShortcut()
        {
            ShowWindow();
        }

        // Register the shortcut for adding bookmark
        [Shortcut("Scene Compass/Add Bookmark", KeyCode.B, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        public static void AddBookmarkShortcut()
        {
            // Get the existing window or create a new one
            if (instance == null)
                instance = GetWindow<BookmarkTool>("Scene Compass - Bookmarks", false);
            
            instance.Focus(); // Bring the window to focus

            // Call the instance method to add a bookmark
            instance.ShowAddBookmarkDialog();
    }

    private void OnEnable()
    {
            instance = this;
            
            dataPath = "Packages/com.sye.scenecompass/Editor/Resources/BookmarkData/bookmarks.json";

            // Load textures
            bannerImage = Resources.Load<Texture2D>("SC_Images/banner");
            syeLogoTexture = Resources.Load<Texture2D>("SC_Images/sye_logo");

            LoadBookmarks();
            RefreshAvailableGroups();

            // Subscribe to scene change events
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        private void OnFocus()
        {
            // Force reload bookmarks when window gains focus
        LoadBookmarks();
            RefreshAvailableGroups();
            RefreshBookmarkListUI();
        }

        private void OnLostFocus()
        {
            // Save expansion states when window loses focus
            SaveBookmarks();
        }

        private void OnDisable()
        {
            // Save expansion states when window is disabled
            SaveBookmarks();

            // Unsubscribe from scene change events
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            RefreshBookmarkListUI();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshBookmarkListUI();
        }

        private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            RefreshBookmarkListUI();
        }

        private void RefreshAvailableGroups()
        {
            // Start with the Default group
            availableGroups = new List<string> { "Default" };
            
            // Add all unique groups from bookmarks
            foreach (var bookmark in bookmarks)
            {
                if (!string.IsNullOrEmpty(bookmark.group) && !availableGroups.Contains(bookmark.group))
                {
                    availableGroups.Add(bookmark.group);
                }
            }
        }

        private void CreateGUI()
        {
            rootElement = rootVisualElement;
            rootElement.Clear(); // Clear any existing UI

            // Style the window with a nice background
            rootElement.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // Add padding to root manually
            rootElement.style.paddingLeft = 10;
            rootElement.style.paddingRight = 10;
            rootElement.style.paddingTop = 0;
            rootElement.style.paddingBottom = 10;

            // --- Banner ---
            if (bannerImage != null)
            {
                var bannerContainer = new VisualElement();
                bannerContainer.style.paddingTop = 0;
                bannerContainer.style.paddingBottom = 0;
                bannerContainer.style.paddingLeft = 0;
                bannerContainer.style.paddingRight = 0;
                bannerContainer.style.marginLeft = -10; // Compensate for root padding
                bannerContainer.style.marginRight = -10; // Compensate for root padding
                bannerContainer.style.marginBottom = 0; // No space below banner
                bannerContainer.style.height = 50;
                bannerContainer.style.minHeight = 50;
                bannerContainer.style.maxHeight = 50;
                bannerContainer.style.backgroundColor = Color.black; // Black background
                bannerContainer.style.flexDirection = FlexDirection.Row;
                bannerContainer.style.justifyContent = Justify.SpaceBetween;
                bannerContainer.style.alignItems = Align.Center;

                var bannerImageElement = new Image { image = bannerImage };
                bannerImageElement.style.width = 400;
                bannerImageElement.style.height = 40;
                bannerImageElement.style.minHeight = 40;
                bannerImageElement.style.maxHeight = 40;
                bannerImageElement.scaleMode = ScaleMode.ScaleToFit;
                bannerContainer.Add(bannerImageElement);

                rootElement.Add(bannerContainer);
            }

            // --- Search Bar ---
            var searchFieldContainer = new VisualElement();
            searchFieldContainer.style.flexDirection = FlexDirection.Row;
            searchFieldContainer.style.marginTop = 0;
            searchFieldContainer.style.marginLeft = -10; // Compensate for root padding
            searchFieldContainer.style.marginRight = -10; // Compensate for root padding
            searchFieldContainer.style.marginBottom = 5; // Space below header
            searchFieldContainer.style.paddingLeft = 10; // Match root padding
            searchFieldContainer.style.paddingRight = 10; // Match root padding
            searchFieldContainer.style.paddingTop = 6;
            searchFieldContainer.style.paddingBottom = 6;
            searchFieldContainer.style.alignItems = Align.Center;
            searchFieldContainer.style.justifyContent = Justify.FlexStart;
            searchFieldContainer.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            searchFieldContainer.style.flexGrow = 0;

            // Search icon - wrapper to ensure vertical alignment
            var searchIconContainer = new VisualElement();
            searchIconContainer.style.width = 16;
            searchIconContainer.style.height = 16;
            searchIconContainer.style.marginRight = 6;
            searchIconContainer.style.alignSelf = Align.Center;
            
            var searchIcon = new Image();
            searchIcon.image = EditorGUIUtility.FindTexture("Search Icon");
            if (searchIcon.image == null)
            {
                searchIcon.image = EditorGUIUtility.FindTexture("d_Search Icon");
            }
            searchIcon.style.width = 16;
            searchIcon.style.height = 16;
            searchIcon.style.opacity = 0.7f;
            searchIcon.style.flexShrink = 0;
            searchIconContainer.Add(searchIcon);
            searchFieldContainer.Add(searchIconContainer);

            // Search field
            var searchFieldWrapper = new VisualElement();
            searchFieldWrapper.style.flexGrow = 1;
            searchFieldWrapper.style.height = 20;
            searchFieldWrapper.style.alignSelf = Align.Center;
            
            searchField = new TextField();
            searchField.value = searchQuery;
            searchField.style.flexGrow = 1;
            searchField.style.height = 20;
            searchField.style.backgroundColor = Color.clear;
            searchField.style.borderTopWidth = 0;
            searchField.style.borderBottomWidth = 0;
            searchField.style.borderLeftWidth = 0;
            searchField.style.borderRightWidth = 0;
            searchField.style.paddingLeft = 0;
            searchField.style.paddingRight = 0;
            searchField.style.paddingTop = 0;
            searchField.style.paddingBottom = 0;
            searchField.style.marginLeft = 0;
            searchField.style.marginRight = 6;
            searchField.style.marginTop = 0;
            searchField.style.marginBottom = 0;
            searchField.style.unityTextAlign = TextAnchor.MiddleLeft;
            searchField.style.color = SceneCompassColors.Secondary;
            searchFieldWrapper.Add(searchField);
            searchFieldContainer.Add(searchFieldWrapper);

            // Add placeholder text
            searchField.RegisterCallback<FocusInEvent>(evt => {
                if (searchField.value == "Search bookmarks or groups...")
                {
                    searchField.value = "";
                    searchField.style.color = SceneCompassColors.Secondary;
                }
            });

            searchField.RegisterCallback<FocusOutEvent>(evt => {
                if (string.IsNullOrEmpty(searchField.value))
                {
                    searchField.value = "Search bookmarks or groups...";
                    searchField.style.color = new Color(0.5f, 0.5f, 0.5f);
                }
            });

            // Set initial placeholder
            if (string.IsNullOrEmpty(searchField.value))
            {
                searchField.value = "Search bookmarks or groups...";
                searchField.style.color = new Color(0.5f, 0.5f, 0.5f);
            }

            // Add search callback
            searchField.RegisterValueChangedCallback(evt => {
                searchQuery = evt.newValue;
                if (searchQuery == "Search bookmarks or groups...")
                    searchQuery = "";
                RefreshBookmarkListUI();
            });

            // Scene filter button - wrapper for alignment
            var sceneFilterContainer = new VisualElement();
            sceneFilterContainer.style.width = 18;
            sceneFilterContainer.style.height = 18;
            sceneFilterContainer.style.marginRight = 6;
            sceneFilterContainer.style.alignSelf = Align.Center;
            
            var sceneFilterImage = new Image();
            
            // Try multiple ways to get the icon
            Texture2D sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            if (sceneIcon == null) 
            {
                sceneIcon = EditorGUIUtility.FindTexture("SceneAsset Icon");
            }
            if (sceneIcon == null)
            {
                sceneIcon = EditorGUIUtility.FindTexture("d_SceneAsset Icon");
            }
            if (sceneIcon == null)
            {
                sceneIcon = EditorGUIUtility.IconContent("GameObject Icon").image as Texture2D;
            }
            
            sceneFilterImage.image = sceneIcon;
            sceneFilterImage.style.width = 18;
            sceneFilterImage.style.height = 18;
            sceneFilterImage.style.flexShrink = 0;
            sceneFilterImage.style.opacity = showCurrentSceneOnly ? 0.4f : 1f;
            
            // Add click handler to the image
            sceneFilterImage.RegisterCallback<MouseUpEvent>(evt => {
                showCurrentSceneOnly = !showCurrentSceneOnly;
                sceneFilterImage.style.opacity = showCurrentSceneOnly ? 0.4f : 1f;
                RefreshBookmarkListUI();
            });
            
            sceneFilterContainer.Add(sceneFilterImage);
            searchFieldContainer.Add(sceneFilterContainer);

            // Add Bookmark Button - wrapper for alignment
            var addButtonContainer = new VisualElement();
            addButtonContainer.style.width = 22;
            addButtonContainer.style.height = 22;
            addButtonContainer.style.alignSelf = Align.Center;
            
            addBookmarkButton = new Button(ShowAddBookmarkDialog);
            addBookmarkButton.text = "+";
            addBookmarkButton.style.width = 22;
            addBookmarkButton.style.height = 22;
            addBookmarkButton.style.minWidth = 22;
            addBookmarkButton.style.minHeight = 22;
            addBookmarkButton.style.backgroundColor = SceneCompassColors.Primary;
            addBookmarkButton.style.color = SceneCompassColors.Secondary;
            addBookmarkButton.style.fontSize = 20;
            addBookmarkButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            addBookmarkButton.style.paddingLeft = 0;
            addBookmarkButton.style.paddingRight = 0;
            addBookmarkButton.style.paddingTop = 0;
            addBookmarkButton.style.paddingBottom = 3; // Adjust for "+" vertical alignment
            addBookmarkButton.style.marginLeft = 0;
            addBookmarkButton.style.marginRight = 0;
            addBookmarkButton.style.marginTop = 0;
            addBookmarkButton.style.marginBottom = 0;
            addBookmarkButton.style.flexShrink = 0;
            
            addButtonContainer.Add(addBookmarkButton);
            searchFieldContainer.Add(addButtonContainer);

            rootElement.Add(searchFieldContainer);

            // --- Scroll View for Bookmarks ---
            scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1; // Allow scroll view to take all available space
            scrollView.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f); // Slightly lighter background
            scrollView.style.minHeight = 100; // Ensure there's always space for bookmarks
            scrollView.style.marginTop = 0;
            scrollView.style.marginBottom = 0;
            scrollView.style.marginLeft = 0;
            scrollView.style.marginRight = 0;

            // Container for the actual bookmark list items
            bookmarkListContainer = new VisualElement();
            bookmarkListContainer.style.paddingLeft = 5;
            bookmarkListContainer.style.paddingRight = 5;
            bookmarkListContainer.style.paddingTop = 5;
            bookmarkListContainer.style.paddingBottom = 5;
            scrollView.Add(bookmarkListContainer);

            rootElement.Add(scrollView);

            // Populate the list initially
            RefreshBookmarkListUI();

            // --- Footer ---
            DrawFooterUIElements();
        }

        private void RefreshBookmarkListUI()
        {
            // Check if UI is initialized
            if (bookmarkListContainer == null)
                return;
                
            bookmarkListContainer.Clear(); // Clear existing UI elements
            groupContainers.Clear();

            // Get current scene GUID
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string currentSceneGUID = AssetDatabase.AssetPathToGUID(currentScene.path);

            // Filter bookmarks by current scene if toggle is on
            var filteredBookmarks = showCurrentSceneOnly 
                ? bookmarks.Where(b => b.sceneGUID == currentSceneGUID).ToList()
                : bookmarks;

            // Apply search filter if query exists
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredBookmarks = filteredBookmarks.Where(b => 
                    b.name.ToLower().Contains(searchQuery.ToLower()) || 
                    b.group.ToLower().Contains(searchQuery.ToLower())
                ).ToList();
            }

            // Group bookmarks
            var bookmarksByGroup = filteredBookmarks.GroupBy(b => string.IsNullOrEmpty(b.group) ? "Default" : b.group);

            foreach (var group in bookmarksByGroup.OrderBy(g => g.Key))
            {
                AddGroupToUI(group.Key, group.ToList());
            }

            // Show message if no bookmarks exist
            if (filteredBookmarks.Count == 0)
            {
                var noBookmarksLabel = new Label(
                    string.IsNullOrEmpty(searchQuery) 
                        ? "No bookmarks found. Click 'Add Bookmark' to create one."
                        : "No bookmarks match your search."
                );
                noBookmarksLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noBookmarksLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                noBookmarksLabel.style.marginTop = 20;
                bookmarkListContainer.Add(noBookmarksLabel);
            }
        }

        private void AddGroupToUI(string groupName, List<BookmarkEntry> groupBookmarks)
        {
            // Create group container with foldout
            var groupContainer = new VisualElement();
            groupContainer.style.marginBottom = 10;

            // Create foldout header with context menu
            var foldout = new Foldout();
            foldout.text = groupName + $" ({groupBookmarks.Count})";
            foldout.value = groupExpansionStates.ContainsKey(groupName) ? groupExpansionStates[groupName] : true; // Use saved state or default to expanded
            foldout.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            foldout.style.color = SceneCompassColors.Secondary;

            // Save state when foldout changes
            foldout.RegisterValueChangedCallback(evt => {
                groupExpansionStates[groupName] = evt.newValue;
                SaveBookmarks();
            });

            // Add context menu to foldout
            foldout.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1) // Right click
                {
                    ShowGroupContextMenu(groupName, groupBookmarks.Count);
                    evt.StopPropagation();
                }
            });
            
            // Add bookmarks to this group
            foreach (var bookmark in groupBookmarks)
            {
                var bookmarkItem = CreateBookmarkItemUI(bookmark);
                foldout.Add(bookmarkItem);
            }

            groupContainer.Add(foldout);
            bookmarkListContainer.Add(groupContainer);
            groupContainers[groupName] = groupContainer;
        }

        private VisualElement CreateBookmarkItemUI(BookmarkEntry bookmark)
        {
            var itemContainer = new VisualElement();
            itemContainer.style.flexDirection = FlexDirection.Row;
            itemContainer.style.alignItems = Align.Center;
            itemContainer.style.marginBottom = 5; // Space between items
            itemContainer.style.paddingTop = 5;
            itemContainer.style.paddingBottom = 5;
            itemContainer.style.paddingLeft = 2;
            itemContainer.style.paddingRight = 2;

            // Background for hover effect
            itemContainer.RegisterCallback<MouseEnterEvent>(evt => {
                itemContainer.style.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
            });
            itemContainer.RegisterCallback<MouseLeaveEvent>(evt => {
                itemContainer.style.backgroundColor = Color.clear;
            });

            // Icon
            GUIContent iconContent = bookmark.type == BookmarkType.Camera
                ? EditorGUIUtility.IconContent("SceneViewCamera")
                : EditorGUIUtility.IconContent("GameObject Icon");

            var iconImage = new Image { image = iconContent.image };
            iconImage.style.width = 16; // Smaller icon
            iconImage.style.height = 16; // Smaller icon
            iconImage.style.marginRight = 5;
            itemContainer.Add(iconImage);

            // Label
            var bookmarkLabel = new Label(bookmark.name);
            bookmarkLabel.style.flexGrow = 1; // Allow label to take available space
            bookmarkLabel.style.color = SceneCompassColors.Secondary; // Secondary text color
            itemContainer.Add(bookmarkLabel);

            // Scene icon (only shown when viewing all scenes and bookmark is from different scene)
            if (!showCurrentSceneOnly)
            {
                var currentSceneGUID = AssetDatabase.AssetPathToGUID(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                if (bookmark.sceneGUID != currentSceneGUID)
                {
                    var sceneIcon = new Image { image = EditorGUIUtility.IconContent("SceneAsset Icon").image };
                    sceneIcon.style.width = 14; // Smaller scene icon
                    sceneIcon.style.height = 14; // Smaller scene icon
                    sceneIcon.style.marginLeft = 5;
                    sceneIcon.style.opacity = 0.7f;
                    itemContainer.Add(sceneIcon);
                }
            }

            // Store the bookmark entry on the UI element for easy access
            itemContainer.userData = bookmark;

            // Add click and context menu handlers
            itemContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left click
                {
                    selectedBookmark = bookmark;
                    GoToBookmark(bookmark);
                    evt.StopPropagation(); // Prevent event from bubbling up
                }
                else if (evt.button == 1) // Right click
                {
                    selectedBookmark = bookmark; // Select on right click too
                    ShowContextMenu(bookmark);
                    evt.StopPropagation();
                }
            });

            return itemContainer;
        }

        private void ShowAddBookmarkDialog()
        {
            var dialog = CreateInstance<BookmarkDialog>();
            dialog.ShowCreateDialog(lastUsedGroup, availableGroups, OnAddBookmarkConfirmed);
        }

        private void ShowEditBookmarkDialog(BookmarkEntry bookmark)
        {
            var dialog = CreateInstance<BookmarkDialog>();
            dialog.ShowEditDialog(bookmark, availableGroups, () => {
                SaveBookmarks();
                RefreshAvailableGroups();
                RefreshBookmarkListUI();
            });
        }

        private void OnAddBookmarkConfirmed(string name, string group, bool createNewGroup, string newGroupName)
    {
        BookmarkEntry entry = new BookmarkEntry();
            
            // Set common properties
            entry.name = name;
            entry.group = createNewGroup ? newGroupName : group;
            
            // Get current scene info
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            entry.sceneGUID = AssetDatabase.AssetPathToGUID(currentScene.path);

            if (Selection.activeGameObject != null)
            {
                entry.type = BookmarkType.GameObject;
                if (string.IsNullOrEmpty(name))
                {
                    entry.name = Selection.activeGameObject.name;
                }

                // Get the Global Object Id of the selected GameObject
                entry.objectGlobalID = GlobalObjectId.GetGlobalObjectIdSlow(Selection.activeGameObject).ToString();
            }
            else
            {
                entry.type = BookmarkType.Camera;
                if (string.IsNullOrEmpty(name))
                {
                    entry.name = "Camera View";
                }

                // Get the current SceneView camera position and rotation
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    entry.cameraPosition = sceneView.camera.transform.position;
                    entry.cameraRotation = sceneView.camera.transform.rotation;
                }
            }

            // Remember the last used group
            lastUsedGroup = entry.group;

            // Add the entry to the list and save
            bookmarks.Add(entry);
        SaveBookmarks();
            RefreshAvailableGroups();

            // Refresh the UI
            RefreshBookmarkListUI();
    }

    private void GoToBookmark(BookmarkEntry bookmark)
    {
            // Get current scene GUID
            var currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string currentSceneGUID = AssetDatabase.AssetPathToGUID(currentScene.path);

            // Check if bookmark is in a different scene
            if (bookmark.sceneGUID != currentSceneGUID)
            {
                // Convert GUID back to path for opening the scene
                string scenePath = AssetDatabase.GUIDToAssetPath(bookmark.sceneGUID);
                if (string.IsNullOrEmpty(scenePath))
                {
                    EditorUtility.DisplayDialog("Scene Not Found", 
                        $"The scene for this bookmark could not be found. It may have been moved or deleted.", 
                        "OK");
                    return;
                }

                // Get the target scene name
                string targetSceneName = Path.GetFileNameWithoutExtension(scenePath);

                // Ask user if they want to open the scene
                if (EditorUtility.DisplayDialog("Open Different Scene?", 
                    $"This bookmark is in scene '{targetSceneName}'. Do you want to open that scene?", 
                    "Yes", "No"))
                {
                    // Save current scene if needed
                    if (currentScene.isDirty)
                    {
                        if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                            "The current scene has unsaved changes. Do you want to save before opening the new scene?", 
                            "Save and Continue", "Cancel"))
                        {
                            return; // User canceled
                        }
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    }

                    // Open the scene
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    return; // User chose not to open the scene
                }
            }

            if (bookmark.type == BookmarkType.Camera)
            {
                // Move the SceneView camera to the saved position and rotation
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    // Set both position and rotation
                    sceneView.pivot = bookmark.cameraPosition;
                    sceneView.rotation = bookmark.cameraRotation;
                    sceneView.camera.transform.position = bookmark.cameraPosition;
                    sceneView.camera.transform.rotation = bookmark.cameraRotation;
                    sceneView.Repaint();
                }
            }
            else if (bookmark.type == BookmarkType.GameObject)
            {
                // Find the GameObject by Global Object Id
                GlobalObjectId globalId;
                if (GlobalObjectId.TryParse(bookmark.objectGlobalID, out globalId))
                {
                    Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);

                        // Move the SceneView camera to the GameObject
                        SceneView sceneView = SceneView.lastActiveSceneView;
                        if (sceneView != null)
                        {
                            sceneView.FrameSelected();
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Bookmark Not Found", "The bookmarked GameObject could not be found in the scene.", "OK");
                    }
                }
            }
        }

        private void ShowContextMenu(BookmarkEntry bookmark)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Go To Bookmark"), false, () => GoToBookmark(bookmark));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Edit..."), false, () => ShowEditBookmarkDialog(bookmark));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete Bookmark", $"Are you sure you want to delete '{bookmark.name}'?", "Yes", "No"))
                {
                    bookmarks.Remove(bookmark);
                    SaveBookmarks();
                    RefreshAvailableGroups();
                    RefreshBookmarkListUI(); // Refresh UI after deletion
                }
            });
            menu.ShowAsContext();
        }

        private void ShowGroupContextMenu(string groupName, int visibleCount)
        {
            GenericMenu menu = new GenericMenu();

            // Add expand/collapse options
            menu.AddItem(new GUIContent("Expand All Groups"), false, () => ExpandAllGroups(true));
            menu.AddItem(new GUIContent("Collapse All Groups"), false, () => ExpandAllGroups(false));
            menu.AddSeparator("");

            // Add rename option
            menu.AddItem(new GUIContent("Rename Group"), false, () => ShowRenameGroupDialog(groupName));

            // Add delete option with confirmation
            menu.AddItem(new GUIContent("Delete Group"), false, () =>
            {
                int totalCount = bookmarks.Count(b => b.group == groupName);
                string message = showCurrentSceneOnly && totalCount > visibleCount
                    ? $"This group contains {totalCount} bookmarks across all scenes. Are you sure you want to delete all of them?"
                    : $"Are you sure you want to delete this group and all its {visibleCount} bookmarks?";

                if (EditorUtility.DisplayDialog("Delete Group", message, "Yes", "No"))
                {
                    bookmarks.RemoveAll(b => b.group == groupName);
                    groupExpansionStates.Remove(groupName);
                    SaveBookmarks();
                    RefreshAvailableGroups();
                    RefreshBookmarkListUI();
                }
            });

            menu.ShowAsContext();
        }

        private void ExpandAllGroups(bool expand)
        {
            foreach (var group in availableGroups)
            {
                groupExpansionStates[group] = expand;
            }
            RefreshBookmarkListUI();
            SaveBookmarks();
        }

        private void ShowRenameGroupDialog(string oldGroupName)
        {
            var dialog = CreateInstance<GroupRenameDialog>();
            dialog.ShowDialog(oldGroupName, (newName) =>
            {
                if (!string.IsNullOrEmpty(newName) && newName != oldGroupName)
                {
                    int totalCount = bookmarks.Count(b => b.group == oldGroupName);
                    int visibleCount = bookmarks.Count(b => b.group == oldGroupName && 
                        (!showCurrentSceneOnly || b.sceneGUID == AssetDatabase.AssetPathToGUID(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path)));

                    if (showCurrentSceneOnly && totalCount > visibleCount)
                    {
                        if (!EditorUtility.DisplayDialog("Rename Group", 
                            $"This group contains {totalCount} bookmarks across all scenes. Do you want to rename all of them?", 
                            "Yes", "No"))
                        {
                            return;
                        }
                    }

                    // Update all bookmarks in this group
                    foreach (var bookmark in bookmarks.Where(b => b.group == oldGroupName))
                    {
                        bookmark.group = newName;
                    }

                    SaveBookmarks();
                    RefreshAvailableGroups();
                    RefreshBookmarkListUI();
                }
            });
        }

        [System.Serializable]
        private class GroupExpansionState
        {
            public string groupName;
            public bool isExpanded;
        }

        [System.Serializable]
        private class BookmarkData
        {
            public List<BookmarkEntry> bookmarks;
            public string lastUsedGroup;
            public List<GroupExpansionState> groupExpansionStates = new List<GroupExpansionState>();
    }

    private void LoadBookmarks()
    {
            bookmarks = new List<BookmarkEntry>(); // Reset bookmarks first
            groupExpansionStates.Clear(); // Reset expansion states
            
        if (File.Exists(dataPath))
            {
                try
        {
            string json = File.ReadAllText(dataPath);
                    BookmarkData data = JsonUtility.FromJson<BookmarkData>(json);
                    if (data != null)
                    {
                        bookmarks = data.bookmarks ?? new List<BookmarkEntry>();
                        lastUsedGroup = data.lastUsedGroup ?? "Default";
                        
                        // Convert serialized expansion states to dictionary
                        if (data.groupExpansionStates != null)
                        {
                            foreach (var state in data.groupExpansionStates)
                            {
                                groupExpansionStates[state.groupName] = state.isExpanded;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to load bookmarks from {dataPath}: {e.Message}");
                }
            }
    }

    private void SaveBookmarks()
    {
            // Convert dictionary to serializable list
            var serializedStates = new List<GroupExpansionState>();
            foreach (var kvp in groupExpansionStates)
            {
                serializedStates.Add(new GroupExpansionState 
                { 
                    groupName = kvp.Key, 
                    isExpanded = kvp.Value 
                });
            }

            BookmarkData data = new BookmarkData 
            { 
                bookmarks = bookmarks,
                lastUsedGroup = lastUsedGroup,
                groupExpansionStates = serializedStates
            };
            
            string json = JsonUtility.ToJson(data, true);
            try
            {
        Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
        File.WriteAllText(dataPath, json);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to save bookmarks to {dataPath}: {e.Message}");
            }
        }

        private void UpdateAddBookmarkButtonLabel()
        {
            try
            {
                var shortcutBinding = ShortcutManager.instance.GetShortcutBinding("Scene Compass/Add Bookmark");
                string shortcutText = shortcutBinding.ToShortcutString();
                if (addBookmarkButton != null)
                {
                    addBookmarkButton.text = $"Add Bookmark ({shortcutText})";
                }
            }
            catch (System.Exception)
            {
                // Fallback if shortcut is not available
                if (addBookmarkButton != null)
                {
                    addBookmarkButton.text = "Add Bookmark (Ctrl+Alt+Shift+B)";
                }
            }
        }

        private void DrawFooterUIElements()
        {
            var footerContainer = new VisualElement();
            footerContainer.style.flexDirection = FlexDirection.Row; // Change to horizontal layout
            footerContainer.style.alignItems = Align.Center; // Center elements vertically
            footerContainer.style.justifyContent = Justify.SpaceBetween; // Space between elements
            footerContainer.style.flexShrink = 0;
            footerContainer.style.flexGrow = 0;
            footerContainer.style.height = 32; // Reduced height
            footerContainer.style.minHeight = 32;
            footerContainer.style.maxHeight = 32;
            footerContainer.style.marginTop = 4; // Small margin above footer
            footerContainer.style.marginBottom = 0;
            footerContainer.style.paddingLeft = 0;
            footerContainer.style.paddingRight = 0;
            footerContainer.style.paddingTop = 0;
            footerContainer.style.paddingBottom = 0;
            footerContainer.style.marginLeft = 0;
            footerContainer.style.marginRight = 0;
            footerContainer.style.borderTopWidth = 1;
            footerContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);

            // Left side: Copyright label
            var copyrightLabel = new Label("© 2024 Simple Yet Efficient");
            copyrightLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            copyrightLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            copyrightLabel.style.fontSize = 10;
            copyrightLabel.style.marginLeft = 4;
            footerContainer.Add(copyrightLabel);

            // Right side: Navigation buttons
            var buttonSection = new VisualElement();
            buttonSection.style.flexDirection = FlexDirection.Row;
            buttonSection.style.alignItems = Align.Center;
            buttonSection.style.justifyContent = Justify.FlexEnd;

            var aboutButton = new Button(() => AboutWindow.ShowWindow());
            aboutButton.text = "About";
            aboutButton.style.backgroundColor = Color.clear;
            aboutButton.style.color = SceneCompassColors.Primary;
            aboutButton.style.marginRight = 5;
            aboutButton.style.paddingLeft = 4;
            aboutButton.style.paddingRight = 4;
            aboutButton.style.paddingTop = 0;
            aboutButton.style.paddingBottom = 0;
            aboutButton.style.height = 20;
            buttonSection.Add(aboutButton);

            var helpButton = new Button(() => HelpWindow.ShowWindow());
            helpButton.text = "Help";
            helpButton.style.backgroundColor = Color.clear;
            helpButton.style.color = SceneCompassColors.Primary;
            helpButton.style.marginRight = 5;
            helpButton.style.paddingLeft = 4;
            helpButton.style.paddingRight = 4;
            helpButton.style.paddingTop = 0;
            helpButton.style.paddingBottom = 0;
            helpButton.style.height = 20;
            buttonSection.Add(helpButton);

            var docsButton = new Button(() => OpenURL("https://github.com/simple-yet-efficient/scene-compass/wiki"));
            docsButton.text = "Docs";
            docsButton.style.backgroundColor = Color.clear;
            docsButton.style.color = SceneCompassColors.Primary;
            docsButton.style.marginRight = 4;
            docsButton.style.paddingLeft = 4;
            docsButton.style.paddingRight = 4;
            docsButton.style.paddingTop = 0;
            docsButton.style.paddingBottom = 0;
            docsButton.style.height = 20;
            buttonSection.Add(docsButton);

            footerContainer.Add(buttonSection);
            rootElement.Add(footerContainer);
        }


        private void OpenURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // On some platforms Process.Start doesn't work for URLs
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    Process.Start("open", url);
                }
                else // Linux
                {
                    Process.Start("xdg-open", url);
                }
            }
        }
    }

    // Dialog for creating/editing bookmarks
    public class BookmarkDialog : EditorWindow
    {
        private string bookmarkName;
        private string selectedGroup;
        private List<string> availableGroups;
        private bool createNewGroup;
        private string newGroupName;
        private System.Action<string, string, bool, string> onCreateConfirmed;
        private System.Action onEditConfirmed;
        private BookmarkEntry bookmarkToEdit;
        private bool isEditMode;
        private BookmarkTool parentWindow;

        public void ShowCreateDialog(string defaultGroup, List<string> groups, System.Action<string, string, bool, string> onConfirmed)
        {
            // Setup window properties
            titleContent = new GUIContent("Add Bookmark");
            minSize = new Vector2(300, 180);
            maxSize = new Vector2(400, 250);
            
            // Store reference to parent window
            parentWindow = BookmarkTool.instance;
            
            // Setup dialog data
            bookmarkName = Selection.activeGameObject != null ? Selection.activeGameObject.name : "Camera View";
            selectedGroup = defaultGroup;
            availableGroups = new List<string>(groups); // Make a copy to avoid reference issues
            onCreateConfirmed = onConfirmed;
            createNewGroup = false;
            newGroupName = "New Group";
            isEditMode = false;
            
            // Show as a popup modal window
            ShowModal();
        }

        public void ShowEditDialog(BookmarkEntry bookmark, List<string> groups, System.Action onConfirmed)
        {
            // Setup window properties
            titleContent = new GUIContent("Edit Bookmark");
            minSize = new Vector2(300, 180);
            maxSize = new Vector2(400, 250);
            
            // Store reference to parent window
            parentWindow = BookmarkTool.instance;
            
            // Setup dialog data
            bookmarkToEdit = bookmark;
            bookmarkName = bookmark.name;
            selectedGroup = bookmark.group;
            availableGroups = new List<string>(groups); // Make a copy to avoid reference issues
            onEditConfirmed = onConfirmed;
            createNewGroup = false;
            newGroupName = "New Group";
            isEditMode = true;
            
            // Show as a popup modal window
            ShowModal();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label(isEditMode ? "Edit Bookmark" : "Add New Bookmark", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Name field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(70));
            bookmarkName = EditorGUILayout.TextField(bookmarkName);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Group selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group:", GUILayout.Width(70));
            
            if (!createNewGroup)
            {
                int currentIndex = availableGroups.IndexOf(selectedGroup);
                if (currentIndex < 0) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(currentIndex, availableGroups.ToArray());
                if (newIndex >= 0 && newIndex < availableGroups.Count)
                {
                    selectedGroup = availableGroups[newIndex];
                }
            }
            else
            {
                newGroupName = EditorGUILayout.TextField(newGroupName);
            }
            
            EditorGUILayout.EndHorizontal();

            // Toggle for creating a new group
            createNewGroup = EditorGUILayout.Toggle("Create New Group", createNewGroup);

            GUILayout.Space(15);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }
            
            if (GUILayout.Button(isEditMode ? "Save" : "Create", GUILayout.Width(100)))
            {
                if (isEditMode)
                {
                    bookmarkToEdit.name = bookmarkName;
                    bookmarkToEdit.group = createNewGroup ? newGroupName : selectedGroup;
                    onEditConfirmed?.Invoke();
                }
                else
                {
                    onCreateConfirmed?.Invoke(bookmarkName, selectedGroup, createNewGroup, newGroupName);
                }
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void OnDestroy()
        {
            // If the parent window exists, make sure it's focused
            if (parentWindow != null)
            {
                parentWindow.Focus();
            }
        }
    }
    
    // Ensure ShortcutExtensions is in the same namespace or accessible
    public static class ShortcutExtensions
    {
        public static string ToShortcutString(this ShortcutBinding binding)
        {
            var combinations = binding.keyCombinationSequence;
            if (!combinations.Any())
                return "Unassigned";

            var parts = new List<string>();
            foreach (var combo in combinations)
            {
                parts.Add(combo.ToString());
            }
            return string.Join(", ", parts);
        }
    }

    // Dialog for renaming groups
    public class GroupRenameDialog : EditorWindow
    {
        private string groupName;
        private System.Action<string> onRenameConfirmed;
        private bool isInitialized = false;

        public void ShowDialog(string currentName, System.Action<string> onConfirmed)
        {
            titleContent = new GUIContent("Rename Group");
            minSize = new Vector2(300, 100);
            maxSize = new Vector2(400, 100);
            
            groupName = currentName;
            onRenameConfirmed = onConfirmed;
            isInitialized = true;
            
            ShowModal();
        }

        private void OnGUI()
        {
            if (!isInitialized) return;

            GUILayout.Space(10);
            GUILayout.Label("Enter new group name:", EditorStyles.boldLabel);
            GUILayout.Space(5);

            groupName = GUILayout.TextField(groupName);
            GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                Close();
            }
            
            if (GUILayout.Button("Rename", GUILayout.Width(100)))
            {
                onRenameConfirmed?.Invoke(groupName);
                Close();
            }
            
            GUILayout.EndHorizontal();
        }
    }
} 