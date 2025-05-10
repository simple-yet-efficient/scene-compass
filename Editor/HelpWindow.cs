using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using UnityEditor.ShortcutManagement;

namespace SyE.SceneCompass.Editor
{
public class HelpWindow : EditorWindow
{
        private Texture2D logoTexture;
        private Texture2D syeLogoTexture;
        
        [MenuItem("Window/Scene Compass/Help", false, 2002)]
    public static void ShowWindow()
    {
            var window = GetWindow<HelpWindow>();
            window.titleContent = new GUIContent("Scene Compass Help");
            window.minSize = new Vector2(600, 500);
            window.maxSize = new Vector2(800, 700);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Load textures
            logoTexture = Resources.Load<Texture2D>("SC_Images/logo");
            syeLogoTexture = Resources.Load<Texture2D>("SC_Images/sye_logo");
            
            // Create dummy textures if actual textures are missing
            if (logoTexture == null)
            {
                logoTexture = CreateDummyTexture(120, 120, Color.white, "SC");
            }
            
            if (syeLogoTexture == null)
            {
                syeLogoTexture = CreateDummyTexture(100, 50, new Color(0.9f, 0.9f, 0.9f), "SyE");
            }
        }
        
        // Helper method to create a dummy texture with text
        private Texture2D CreateDummyTexture(int width, int height, Color baseColor, string text)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            // Fill with base color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = baseColor;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Style the window with a nice background
            root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

            // Add padding to root manually
            root.style.paddingLeft = 20;
            root.style.paddingRight = 20;
            root.style.paddingTop = 20;
            root.style.paddingBottom = 20;

            var container = new ScrollView();
            container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            // Add padding to container manually
            container.style.paddingLeft = 20;
            container.style.paddingRight = 20;
            container.style.paddingTop = 20;
            container.style.paddingBottom = 20;
            root.Add(container);

            // Header section with logo and title
            var headerSection = new VisualElement();
            headerSection.style.flexDirection = FlexDirection.Row;
            headerSection.style.marginBottom = 20;
            headerSection.style.alignItems = Align.Center;
            
            // Add logo if available
            if (logoTexture != null)
            {
                var logoContainer = new VisualElement();
                logoContainer.style.width = 80;
                logoContainer.style.height = 80;
                logoContainer.style.marginRight = 15;
                logoContainer.style.overflow = Overflow.Hidden;
                logoContainer.style.backgroundColor = SceneCompassColors.Primary;
                
                var logoImage = new Image { image = logoTexture };
                logoImage.style.width = Length.Percent(100);
                logoImage.style.height = Length.Percent(100);
                logoImage.scaleMode = ScaleMode.ScaleToFit;
                logoContainer.Add(logoImage);
                headerSection.Add(logoContainer);
            }
            
            var titleContainer = new VisualElement();
            
            // Title
            var title = new Label("Scene Compass Help");
            title.style.fontSize = 24;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = SceneCompassColors.Primary;
            titleContainer.Add(title);
            
            // Version 
            var version = new Label("Version 1.0");
            version.style.fontSize = 12;
            version.style.color = SceneCompassColors.Secondary;
            titleContainer.Add(version);
            
            headerSection.Add(titleContainer);
            container.Add(headerSection);

            // Introduction
            AddSection(container, "Introduction", 
                "Scene Compass is a lightweight Unity editor tool that helps you measure, bookmark, and navigate your scenes efficiently.");
                
            // Documentation Link
            var docsLink = new Button(() => OpenURL("https://github.com/simple-yet-efficient/scene-compass/wiki"));
            docsLink.text = "View Documentation on GitHub →";
            docsLink.style.alignSelf = Align.FlexStart;
            docsLink.style.marginBottom = 20;
            docsLink.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
            docsLink.style.color = SceneCompassColors.Primary;
            container.Add(docsLink);

            // Keyboard Shortcuts
            AddSection(container, "Keyboard Shortcuts", null);
            AddShortcut(container, "M (hold)", "Activate Measure Mode");
            AddShortcut(container, "Shift+M (hold)", "Activate Path Measurement Mode");
            AddShortcut(container, $"({ShortcutManager.instance.GetShortcutBinding("Scene Compass/Open Bookmarks Window").ToShortcutString()})", "Open Bookmarks Window");
            AddShortcut(container, $"({ShortcutManager.instance.GetShortcutBinding("Scene Compass/Add Bookmark").ToShortcutString()})", "Add Bookmark");

            // Add bigger separator and more space
            AddBiggerSeparator(container);

            // Tips and Best Practices
            AddSection(container, "Tips & Best Practices", null);
            AddTip(container, "Hold the 'M' key and click to measure distances between points.");
            AddTip(container, "Hold 'Shift+M' and click multiple times to create connected measurements for complex paths.");
            AddTip(container, "Right-click while holding 'M' to clear current measurements.");
            AddTip(container, "Hold 'Ctrl' while clicking in measure mode to snap to object centers.");
            AddTip(container, "Bookmark frequently accessed locations for quick navigation.");
            AddTip(container, "Use bookmark groups to organize your scene references.");

            // Add separator before footer
            AddSeparator(container);

            // Contact section with SyE logo
            var contactSection = new VisualElement();
            contactSection.style.marginTop = 20;
            contactSection.style.marginBottom = 10;
            contactSection.style.flexDirection = FlexDirection.Row;
            contactSection.style.justifyContent = Justify.SpaceBetween;
            contactSection.style.alignItems = Align.Center;
            
            var contactInfo = new VisualElement();
            
            var contactLabel = new Label("Contact Support:");
            contactLabel.style.marginBottom = 5;
            contactInfo.Add(contactLabel);
            
            var emailButton = new Button(() => OpenURL("mailto:aqaddora96@gmail.com"));
            emailButton.text = "aqaddora96@gmail.com";
            
            emailButton.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
            emailButton.style.color = SceneCompassColors.Primary;
            contactInfo.Add(emailButton);
            
            contactSection.Add(contactInfo);
            
            // Add SyE logo on the right
            if (syeLogoTexture != null)
            {
                var syeLogoImage = new Image { image = syeLogoTexture };
                syeLogoImage.style.width = 100;
                syeLogoImage.style.height = 50;
                contactSection.Add(syeLogoImage);
            }
            
            container.Add(contactSection);
            
            // Copyright footer
            var footer = new Label("© 2024 Simple Yet Efficient. All rights reserved.");
            footer.style.unityTextAlign = TextAnchor.MiddleCenter;
            footer.style.marginTop = 10;
            footer.style.color = new Color(0.6f, 0.6f, 0.6f);
            footer.style.fontSize = 10;
            container.Add(footer);
        }

        private void AddBiggerSeparator(VisualElement container)
        {
            // Add extra space
            var spacer = new VisualElement();
            spacer.style.height = 15;
            container.Add(spacer);
            
            // Add thicker separator
            var separator = new VisualElement();
            separator.style.height = 2;
            separator.style.marginTop = 10;
            separator.style.marginBottom = 25;
            separator.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            container.Add(separator);
        }

        private void AddSeparator(VisualElement container)
        {
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = 10;
            separator.style.marginBottom = 10;
            separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            container.Add(separator);
        }

        private void AddSection(VisualElement container, string title, string description)
        {
            var section = new VisualElement();
            section.style.marginBottom = 20;

            var header = new Label(title);
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            header.style.color = SceneCompassColors.Primary;
            section.Add(header);

            if (!string.IsNullOrEmpty(description))
            {
                var desc = new Label(description);
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.marginBottom = 10;
                section.Add(desc);
            }

            container.Add(section);
        }

        private void AddShortcut(VisualElement container, string shortcut, string description)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 5;

            var keyLabel = new Label(shortcut);
            keyLabel.style.width = 80;
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyLabel.style.color = SceneCompassColors.Secondary;
            
            var descLabel = new Label(description);
            descLabel.style.color = SceneCompassColors.Secondary;
            
            row.Add(keyLabel);
            row.Add(descLabel);
            container.Add(row);
        }

        private void AddTip(VisualElement container, string tip)
        {
            var tipElement = new Label("• " + tip);
            tipElement.style.whiteSpace = WhiteSpace.Normal;
            tipElement.style.marginBottom = 5;
            tipElement.style.marginLeft = 15;
            tipElement.style.color = SceneCompassColors.Secondary;
            container.Add(tipElement);
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
} 