using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;

namespace SyE.SceneCompass.Editor
{
public class AboutWindow : EditorWindow
{
        private Texture2D logoTexture;
        private Texture2D bannerImage;
        private Texture2D syeLogoTexture;

        [MenuItem("Window/Scene Compass/About", false, 2003)]
    public static void ShowWindow()
    {
            var window = GetWindow<AboutWindow>();
            window.titleContent = new GUIContent("About Scene Compass");
            window.minSize = new Vector2(600, 700);
            window.maxSize = new Vector2(600, 750);
            window.Show();
    }

    private void OnEnable()
    {
            // Load textures
            logoTexture = Resources.Load<Texture2D>("SC_Images/logo");
            bannerImage = Resources.Load<Texture2D>("SC_Images/banner");
            syeLogoTexture = Resources.Load<Texture2D>("SC_Images/sye_logo");

            // Create dummy textures if actual textures are missing
            if (logoTexture == null)
            {
                logoTexture = CreateDummyTexture(200, 200, Color.white, "Scene Compass");
            }

            if (bannerImage == null)
            {
                bannerImage = CreateDummyTexture(600, 100, new Color(0.3f, 0.6f, 0.9f), "Banner");
            }

            if (syeLogoTexture == null)
            {
                syeLogoTexture = CreateDummyTexture(120, 60, new Color(0.9f, 0.9f, 0.9f), "SyE");
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

            // Add border
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                    {
                        pixels[y * width + x] = Color.gray;
                    }
                }
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

            // Add logo if available - make it smaller
            if (logoTexture != null)
            {
                var logoContainer = new VisualElement();
                logoContainer.style.alignSelf = Align.Center;
                logoContainer.style.marginBottom = 20;
                logoContainer.style.overflow = Overflow.Hidden;
                logoContainer.style.borderTopLeftRadius = 15;
                logoContainer.style.borderTopRightRadius = 15;
                logoContainer.style.borderBottomLeftRadius = 15;
                logoContainer.style.borderBottomRightRadius = 15;
                logoContainer.style.width = 160;
                logoContainer.style.height = 160;
                logoContainer.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f); // #0B1016 dark navy
                
                var logoImage = new Image { image = logoTexture };
                logoImage.style.width = Length.Percent(100);
                logoImage.style.height = Length.Percent(100);
                logoImage.scaleMode = ScaleMode.ScaleToFit;
                logoContainer.Add(logoImage);
                container.Add(logoContainer);
            }
            // Title
            var title = new Label("Scene Compass");
            title.style.fontSize = 24;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 10;
            title.style.color = SceneCompassColors.Primary;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(title);

            // Version
            var version = new Label("Version 1.0");
            version.style.fontSize = 14;
            version.style.unityTextAlign = TextAnchor.MiddleCenter;
            version.style.marginBottom = 10;
            version.style.color = SceneCompassColors.Secondary;
            container.Add(version);

            // Description of the plugin
            var description = new Label("Measure, bookmark, and navigate your scenes efficiently without breaking your flow.");
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.unityTextAlign = TextAnchor.MiddleCenter;
            description.style.marginBottom = 20;
            description.style.color = SceneCompassColors.Secondary;
            container.Add(description);

            // Feature description bullets
            var featureDescription = new VisualElement();
            featureDescription.style.marginBottom = 20;
            
            AddFeatureBullet(featureDescription, "Measure Tool", "Hold M to measure distances with precision, Shift+M for path measurements");
            AddFeatureBullet(featureDescription, "Bookmark System", "Save and organize important positions and GameObjects");
            
            container.Add(featureDescription);

            // Button container for Help and Documentation
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignSelf = Align.Center;
            buttonContainer.style.marginBottom = 20;

            // Help button
            var helpButton = new Button(() => EditorApplication.ExecuteMenuItem("Window/Scene Compass/Help"));
            helpButton.text = "Help";
            helpButton.style.marginRight = 10;
            helpButton.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
            helpButton.style.color = SceneCompassColors.Primary;
            buttonContainer.Add(helpButton);

            // Documentation link
            var docsLink = new Button(() => OpenURL("https://github.com/simple-yet-efficient/scene-compass/wiki"));
            docsLink.text = "Documentation (GitHub)";
            docsLink.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
            docsLink.style.color = SceneCompassColors.Primary;
            buttonContainer.Add(docsLink);

            container.Add(buttonContainer);

            // Separator
            AddSeparator(container);

            // About Us
            AddSectionTitle(container, "About Us");

            // SyE Logo next to tagline
            var aboutHeader = new VisualElement();
            aboutHeader.style.flexDirection = FlexDirection.Row;
            aboutHeader.style.alignItems = Align.Center;
            aboutHeader.style.marginBottom = 15;

            if (syeLogoTexture != null)
            {
                var syeLogoImage = new Image { image = syeLogoTexture };
                syeLogoImage.style.width = 120;
                syeLogoImage.style.height = 60;
                syeLogoImage.style.marginRight = 15;
                aboutHeader.Add(syeLogoImage);
            }

            var taglineContainer = new VisualElement();
            var tagline = new Label("Simple yet efficient!");
            tagline.style.fontSize = 16;
            tagline.style.unityFontStyleAndWeight = FontStyle.Bold;
            tagline.style.color = SceneCompassColors.Primary;
            taglineContainer.Add(tagline);
            aboutHeader.Add(taglineContainer);

            container.Add(aboutHeader);

            var aboutText = new Label("Simple Yet Efficient is a team focused on delivering practical and straightforward Unity solutions. We create intuitive plugins and assets that simplify complex tasks, making development easier and more efficient.");
            aboutText.style.whiteSpace = WhiteSpace.Normal;
            aboutText.style.marginBottom = 20;
            aboutText.style.color = SceneCompassColors.Secondary;
            container.Add(aboutText);

            // Separator
            AddSeparator(container);

            // Our Assets
            AddSectionTitle(container, "Our Available Assets");

            // Asset Cards (if any)
            // You can add your own assets here using the AddAssetCard method

            // Example Asset Card
            AddAssetCard(
                container,
                Resources.Load<Texture2D>("SC_Images/rest-express-thumb"), // Replace with actual thumbnail texture if available
                "REST Express",
                "Transform Postman collections into Unity-ready API clients instantly. Test API calls in-editor and generate production-ready C# code in minutes.",
                "https://assetstore.unity.com/packages/slug/319060"
            );

            // Example Asset Card
            AddAssetCard(
                container,
                Resources.Load<Texture2D>("SC_Images/thumb_sign_in"), // Replace with actual thumbnail texture if available
                "Universal Integration for Google Sign-In",
                "An easy solution for integrating Google Sign-In into your Unity projects. Supports multiple platforms.",
                "https://assetstore.unity.com/packages/slug/293326"
            );

            // Another Asset Card
            AddAssetCard(
                container,
                Resources.Load<Texture2D>("SC_Images/thumb_biometrics"), // Replace with actual thumbnail texture if available
                "Biometric Authentication Plugin for Unity",
                "Add biometric authentication to your Unity projects. Supports iOS, macOS, Android, and WebGL.",
                "https://assetstore.unity.com/packages/slug/293752"
            );

            // Copyright at bottom
            var copyright = new Label("© 2024 Simple Yet Efficient. All rights reserved.");
            copyright.style.unityTextAlign = TextAnchor.MiddleCenter;
            copyright.style.marginTop = 20;
            copyright.style.color = new Color(0.6f, 0.6f, 0.6f);
            copyright.style.fontSize = 12;
            container.Add(copyright);
        }

        private void AddSectionTitle(VisualElement container, string title)
        {
            var label = new Label(title);
            label.style.fontSize = 18;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 10;
            label.style.marginBottom = 15;
            label.style.color = SceneCompassColors.Primary;
            container.Add(label);
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

        private void AddAssetCard(VisualElement container, Texture2D thumbnail, string title, string description, string url)
        {
            var card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.marginBottom = 20;
            card.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            // Add padding to card manually
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;

            // Add thumbnail if available
            if (thumbnail != null)
            {
                var thumbImage = new Image { image = thumbnail };
                thumbImage.style.width = 100;
                thumbImage.style.height = 100;
                thumbImage.style.marginRight = 15;
                card.Add(thumbImage);
            }

            // Content container
            var content = new VisualElement();
            content.style.flexGrow = 1;

            // Title
            var assetTitle = new Label(title);
            assetTitle.style.fontSize = 16;
            assetTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            assetTitle.style.color = SceneCompassColors.Primary;
            assetTitle.style.marginBottom = 5;
            content.Add(assetTitle);

            // Description
            var assetDesc = new Label(description);
            assetDesc.style.whiteSpace = WhiteSpace.Normal;
            assetDesc.style.marginBottom = 10;
            assetDesc.style.color = SceneCompassColors.Secondary;
            content.Add(assetDesc);

            // URL button
            var urlButton = new Button(() => OpenURL(url));
            urlButton.text = "View in Asset Store";
            urlButton.style.alignSelf = Align.FlexStart;
            urlButton.style.backgroundColor = new Color(0.04f, 0.06f, 0.09f);
            urlButton.style.color = SceneCompassColors.Primary;
            content.Add(urlButton);

            card.Add(content);
            container.Add(card);
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

        // Helper method to add a feature bullet point
        private void AddFeatureBullet(VisualElement container, string title, string description)
        {
            var bulletContainer = new VisualElement();
            bulletContainer.style.flexDirection = FlexDirection.Row;
            bulletContainer.style.marginBottom = 5;
            
            // Bullet point
            var bullet = new Label("•");
            bullet.style.width = 15;
            bullet.style.unityFontStyleAndWeight = FontStyle.Bold;
            bullet.style.color = SceneCompassColors.Primary;
            bulletContainer.Add(bullet);
            
            var textContainer = new VisualElement();
            textContainer.style.flexGrow = 1;
            
            // Feature title
            var featureTitle = new Label(title);
            featureTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            featureTitle.style.color = SceneCompassColors.Primary;
            textContainer.Add(featureTitle);
            
            // Feature description
            var featureDesc = new Label(description);
            featureDesc.style.color = SceneCompassColors.Secondary;
            featureDesc.style.whiteSpace = WhiteSpace.Normal;
            textContainer.Add(featureDesc);
            
            bulletContainer.Add(textContainer);
            container.Add(bulletContainer);
        }
    }
} 