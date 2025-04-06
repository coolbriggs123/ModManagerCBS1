using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using MelonLoader.Preferences;

[assembly: MelonInfo(typeof(ModManager_.Core), "ModManager+", "1.0.0", "rbawe", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ModManager_
{
    public class Core : MelonMod
    {
        private bool isGuiVisible = false;
        private Rect windowRect = new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600);
        private Vector2 scrollPosition;
        private readonly string modsPath = Path.Combine(Directory.GetCurrentDirectory(), "Mods");
        private List<(string path, bool isEnabled)> modFiles = new List<(string path, bool isEnabled)>();
        private GUIStyle buttonStyle;
        private GUIStyle categoryStyle;
        private GUIStyle headerStyle;
        private GUIStyle optionStyle;
        private GUIStyle windowStyle;
        private bool stylesInitialized = false;
        private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();
        private Dictionary<string, float> tempSliderValues = new Dictionary<string, float>();
        private string selectedTab = "Mods";
        private GameObject settingsButton;
        private GameObject modSettingsButton;
        private bool restartRequired = false;
        private Vector2 modListScrollPosition;
        private Vector2 prefsScrollPosition;
        private string selectedMod = "";
        private GUIStyle modListStyle;
        private GUIStyle modListButtonStyle;
        private GUIStyle categoryBoxStyle;
        private GUIStyle selectedModStyle;
        private bool isCapturingKey = false;
        private MelonPreferences_Entry currentCapturingEntry = null;
        private float dragStartValue = 0f;
        private bool isDragging = false;
        private Vector2 dragStartMousePos;
        private bool buttonInitialized = false;

        private void ToggleGui()
        {
            isGuiVisible = !isGuiVisible;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main")
            {
                // Clear button references when entering main scene
                settingsButton = null;
                modSettingsButton = null;
                buttonInitialized = false;
            }
            else if (sceneName == "Menu")
            {
                if (!buttonInitialized)
                {
                    MelonCoroutines.Start(MenuDelayedInit());
                }
            }
        }

        private System.Collections.IEnumerator MenuDelayedInit()
        {
            // Wait for 3 seconds before trying to find the settings button
            yield return new WaitForSeconds(3f);
            
            // Only proceed if we haven't initialized and we're still in the menu scene
            if (!buttonInitialized && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu")
            {
                FindSettingsButton();
            }
        }

        private void FindSettingsButton()
        {
            // Clean up any existing mod settings button
            if (modSettingsButton != null)
            {
                GameObject.Destroy(modSettingsButton);
                modSettingsButton = null;
            }

            // Method 1: Try to find by component type and text content
            Button[] allButtons = GameObject.FindObjectsOfType<Button>();
            foreach (Button btn in allButtons)
            {
                // Check for TextMeshPro text component
                TextMeshProUGUI tmpText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null && tmpText.text.ToLower().Contains("settings"))
                {
                    settingsButton = btn.gameObject;
                    CreateModSettingsButton();
                    buttonInitialized = true;
                    return;
                }

                // Fallback: Check for regular Text component
                Text regularText = btn.GetComponentInChildren<Text>();
                if (regularText != null && regularText.text.ToLower().Contains("settings"))
                {
                    settingsButton = btn.gameObject;
                    CreateModSettingsButton();
                    buttonInitialized = true;
                    return;
                }
            }

            // If we couldn't find the settings button, create a floating button instead
            LoggerInstance.Msg("Could not find settings button, will use floating button instead.");
            CreateFloatingModSettingsButton();
            buttonInitialized = true;
        }

        private void CreateFloatingModSettingsButton()
        {
            // Implementation for creating a floating button when settings button can't be found
            // This is a fallback method that you can implement if needed
        }

        private void CreateModSettingsButton()
        {
            if (settingsButton == null) return;

            // Create our mod settings button as a copy of the settings button
            modSettingsButton = GameObject.Instantiate(settingsButton, settingsButton.transform.parent);
            modSettingsButton.name = "ModSettings";
            
            // Position it next to the settings button
            RectTransform settingsRect = settingsButton.GetComponent<RectTransform>();
            RectTransform modSettingsRect = modSettingsButton.GetComponent<RectTransform>();
            
            if (settingsRect != null && modSettingsRect != null)
            {
                // Copy the settings button's properties
                modSettingsRect.anchorMin = settingsRect.anchorMin;
                modSettingsRect.anchorMax = settingsRect.anchorMax;
                modSettingsRect.pivot = settingsRect.pivot;
                modSettingsRect.sizeDelta = settingsRect.sizeDelta;
                
                // Position it to the right of the settings button
                Vector2 position = settingsRect.anchoredPosition;
                position.x = (settingsRect.sizeDelta.x + 100);
                modSettingsRect.anchoredPosition = position;

                // Ensure it's on the same Z level
                Vector3 pos = modSettingsButton.transform.localPosition;
                pos.z = settingsButton.transform.localPosition.z;
                modSettingsButton.transform.localPosition = pos;
                
                // Add layout element
                LayoutElement layoutElement = modSettingsButton.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;
                
                // Update button
                Button button = modSettingsButton.GetComponent<Button>();
                if (button != null)
                {
                    // Copy the original button's colors
                    Button originalButton = settingsButton.GetComponent<Button>();
                    if (originalButton != null)
                    {
                        button.colors = originalButton.colors;
                    }

                    // Copy all images from the original button
                    Image[] originalImages = settingsButton.GetComponents<Image>();
                    Image[] newImages = modSettingsButton.GetComponents<Image>();
                    
                    for (int i = 0; i < originalImages.Length && i < newImages.Length; i++)
                    {
                        if (originalImages[i] != null && newImages[i] != null)
                        {
                            newImages[i].sprite = originalImages[i].sprite;
                            newImages[i].type = originalImages[i].type;
                            newImages[i].color = originalImages[i].color;
                            newImages[i].material = originalImages[i].material;
                            newImages[i].raycastTarget = originalImages[i].raycastTarget;
                        }
                    }

                    // Copy child images
                    Image[] originalChildImages = settingsButton.GetComponentsInChildren<Image>(true);
                    Image[] newChildImages = modSettingsButton.GetComponentsInChildren<Image>(true);
                    
                    for (int i = 0; i < originalChildImages.Length && i < newChildImages.Length; i++)
                    {
                        if (originalChildImages[i] != null && newChildImages[i] != null)
                        {
                            newChildImages[i].sprite = originalChildImages[i].sprite;
                            newChildImages[i].type = originalChildImages[i].type;
                            newChildImages[i].color = originalChildImages[i].color;
                            newChildImages[i].material = originalChildImages[i].material;
                            newChildImages[i].raycastTarget = originalChildImages[i].raycastTarget;
                        }
                    }

                    // Update text while preserving style
                    TextMeshProUGUI originalTmpText = settingsButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    TextMeshProUGUI tmpText = modSettingsButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (originalTmpText != null && tmpText != null)
                    {
                        tmpText.font = originalTmpText.font;
                        tmpText.fontSize = originalTmpText.fontSize;
                        tmpText.fontStyle = originalTmpText.fontStyle;
                        tmpText.color = originalTmpText.color;
                        tmpText.material = originalTmpText.material;
                        tmpText.alignment = originalTmpText.alignment;
                        tmpText.text = "Mod Settings";
                    }
                    else
                    {
                        Text originalText = settingsButton.GetComponentInChildren<Text>(true);
                        Text regularText = modSettingsButton.GetComponentInChildren<Text>(true);
                        if (originalText != null && regularText != null)
                        {
                            regularText.font = originalText.font;
                            regularText.fontSize = originalText.fontSize;
                            regularText.fontStyle = originalText.fontStyle;
                            regularText.color = originalText.color;
                            regularText.material = originalText.material;
                            regularText.alignment = originalText.alignment;
                            regularText.text = "Mod Settings";
                        }
                    }

                    // Enable the button
                    modSettingsButton.SetActive(true);

                    // Clear existing listeners and add our new one
                    button.onClick = new Button.ButtonClickedEvent();
                    button.onClick.AddListener((UnityEngine.Events.UnityAction)delegate {
                        ToggleGui();
                        if (UnityEngine.EventSystems.EventSystem.current != null)
                        {
                            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                        }
                    });
                }

                // Copy the original CanvasGroup settings if they exist
                CanvasGroup originalCanvasGroup = settingsButton.GetComponent<CanvasGroup>();
                CanvasGroup canvasGroup = modSettingsButton.GetComponent<CanvasGroup>();
                if (originalCanvasGroup != null)
                {
                    if (canvasGroup == null)
                    {
                        canvasGroup = modSettingsButton.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.alpha = originalCanvasGroup.alpha;
                    canvasGroup.blocksRaycasts = originalCanvasGroup.blocksRaycasts;
                    canvasGroup.interactable = originalCanvasGroup.interactable;
                    canvasGroup.ignoreParentGroups = originalCanvasGroup.ignoreParentGroups;
                }
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Modern button style with rounded corners and better hover effects
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(4, 4, 4, 4),
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f), 3),
                    textColor = new Color(0.9f, 0.9f, 0.9f)
                },
                hover = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.25f, 0.25f, 0.25f, 0.95f), 3),
                    textColor = Color.white
                },
                active = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f), 3),
                    textColor = new Color(0.8f, 0.8f, 0.8f)
                }
            };

            categoryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(5, 5, 8, 8),
                margin = new RectOffset(0, 0, 0, 10),
                normal = { 
                    textColor = new Color(0.9f, 0.9f, 1f),
                    background = CreateRoundedRectTexture(2, 2, new Color(0.2f, 0.2f, 0.25f, 0.7f), 3)
                },
                fixedHeight = 35
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white },
                padding = new RectOffset(0, 0, 15, 15)
            };

            optionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                padding = new RectOffset(10, 10, 5, 5)
            };

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(15, 15, 15, 15),
                normal = { 
                    background = CreateRoundedRectTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.97f), 5),
                    textColor = Color.white 
                },
                onNormal = { 
                    background = CreateRoundedRectTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.97f), 5),
                    textColor = Color.white 
                },
                border = new RectOffset(8, 8, 8, 8),
                fontSize = 16
            };

            modListStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 10, 0, 0),
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.12f, 0.12f, 0.12f, 0.95f), 3),
                    textColor = Color.white
                }
            };

            modListButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 8, 8),
                margin = new RectOffset(0, 0, 2, 2),
                fixedHeight = 35,
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.18f, 0.18f, 0.18f, 0.4f), 3),
                    textColor = new Color(0.8f, 0.8f, 0.8f)
                },
                hover = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.22f, 0.22f, 0.22f, 0.9f), 3),
                    textColor = Color.white
                },
                active = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f), 3),
                    textColor = Color.white
                }
            };

            selectedModStyle = new GUIStyle(modListButtonStyle)
            {
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.25f, 0.25f, 0.35f, 0.95f), 3),
                    textColor = Color.white
                },
                hover = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.27f, 0.27f, 0.37f, 1f), 3),
                    textColor = Color.white
                }
            };

            categoryBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 12, 12),
                margin = new RectOffset(0, 0, 0, 10),
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.15f, 0.15f, 0.15f, 0.95f), 5),
                    textColor = Color.white
                }
            };

            // Add slider styles
            GUI.skin.horizontalSlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                fixedHeight = 20,
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.9f), 3)
                }
            };

            GUI.skin.horizontalSliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                fixedHeight = 20,
                fixedWidth = 20,
                normal = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f), 3)
                },
                hover = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f), 3)
                },
                active = {
                    background = CreateRoundedRectTexture(2, 2, new Color(0.4f, 0.4f, 0.4f, 1f), 3)
                }
            };

            stylesInitialized = true;
        }

        // Helper method to create rounded rectangle textures
        private Texture2D CreateRoundedRectTexture(int width, int height, Color color, int radius)
        {
            width = Mathf.Max(width, radius * 2);
            height = Mathf.Max(height, radius * 2);
            
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            
            // Fill the entire texture with the base color first
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            // Create rounded corners by setting appropriate pixels to transparent
            for (int x = 0; x < radius; x++)
            {
                for (int y = 0; y < radius; y++)
                {
                    float distance = Mathf.Sqrt(x * x + y * y);
                    float alpha = distance <= radius ? 1f : 0f;

                    // Top-left corner
                    colors[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    
                    // Top-right corner
                    colors[y * width + (width - 1 - x)] = new Color(color.r, color.g, color.b, color.a * alpha);
                    
                    // Bottom-left corner
                    colors[(height - 1 - y) * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    
                    // Bottom-right corner
                    colors[(height - 1 - y) * width + (width - 1 - x)] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        // Update MakeTex to use the new rounded rectangle method
        private Texture2D MakeTex(int width, int height, Color col)
        {
            return CreateRoundedRectTexture(width, height, col, 3);
        }

        public override void OnGUI()
        {
            if (!isGuiVisible) return;

            // Handle key capture
            if (isCapturingKey && Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                KeyCode pressedKey = Event.current.keyCode;
                if (pressedKey != KeyCode.None && currentCapturingEntry != null)
                {
                    currentCapturingEntry.BoxedValue = pressedKey;
                    MelonPreferences.Save();
                    isCapturingKey = false;
                    currentCapturingEntry = null;
                    Event.current.Use(); // Consume the event
                }
            }

            // Only show the floating button if we haven't found the settings button
            if (modSettingsButton == null)
            {
                InitializeStyles();

                float buttonWidth = 120;
                float buttonHeight = 30;
                float topmargin = 16;
                float sidemargin = 10;
                
                Rect buttonRect = new Rect(
                    Screen.width - buttonWidth - sidemargin,
                    Screen.height - buttonHeight - topmargin,
                    buttonWidth,
                    buttonHeight
                );

                if (GUI.Button(buttonRect, "Mod Settings", buttonStyle))
                {
                    isGuiVisible = !isGuiVisible;
                }
            }

            // Draw the settings window if visible
            if (isGuiVisible)
            {
                InitializeStyles();
                
                // Semi-transparent black background
                GUI.color = new Color(0, 0, 0, 0.85f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                windowRect = GUI.Window(0, windowRect, (GUI.WindowFunction)delegate(int id) 
                {
                    DrawWindow(id);
                }, "", windowStyle);
            }
        }

        private void DrawWindow(int windowID)
        {
            // Removed duplicate "Settings" label
            GUILayout.Space(10);

            // Tabs
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedTab == "Mods", "Mods", buttonStyle)) selectedTab = "Mods";
            if (GUILayout.Toggle(selectedTab == "Config", "Config", buttonStyle)) selectedTab = "Config";
            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Content area with dark semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.color = Color.white;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (selectedTab == "Mods")
            {
                DrawModsSection();
            }
            else
            {
                DrawPrefsSection();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Close button at the bottom
            GUILayout.Space(10);
            if (GUILayout.Button("Close", buttonStyle, GUILayout.Height(40)))
            {
                isGuiVisible = false;
            }

            if (restartRequired)
            {
                GUILayout.Space(5);
                if (GUILayout.Button("Restart Game", buttonStyle, GUILayout.Height(40)))
                {
                    Application.Quit();
                }
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 40));
        }

        private void DrawModsSection()
        {
            foreach (var modInfo in modFiles)
            {
                // Get base name without .disabled extension if it exists
                string modName = Path.GetFileNameWithoutExtension(
                    modInfo.path.EndsWith(".disabled") 
                        ? Path.GetFileNameWithoutExtension(modInfo.path) 
                        : modInfo.path
                );
                
                bool isEnabled = !modInfo.path.EndsWith(".disabled");

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(modName, optionStyle, GUILayout.Width(300));
                
                GUI.color = isEnabled ? Color.green : Color.red;
                if (GUILayout.Button(isEnabled ? "Enabled" : "Disabled", buttonStyle, GUILayout.Width(120)))
                {
                    try
                    {
                        if (isEnabled)
                        {
                            // Disable: rename to .dll.disabled
                            File.Move(modInfo.path, modInfo.path + ".disabled");
                        }
                        else
                        {
                            // Enable: remove .disabled extension
                            File.Move(modInfo.path, modInfo.path.Replace(".disabled", ""));
                        }
                        restartRequired = true;
                        RefreshModList();
                        LoggerInstance.Msg($"Mod {modName} {(isEnabled ? "disabled" : "enabled")}. Restart required.");
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Failed to {(isEnabled ? "disable" : "enable")} {modName}: {ex.Message}");
                    }
                }
                GUI.color = Color.white;
                
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            if (restartRequired)
            {
                GUILayout.Space(20);
                GUI.color = Color.yellow;
                GUILayout.Label("Game restart required for changes to take effect!", headerStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawPrefsSection()
        {
            try
            {
                GUILayout.BeginHorizontal();

                // Left column - Mod List with fixed width
                GUILayout.BeginVertical(modListStyle, GUILayout.Width(200));
                modListScrollPosition = GUILayout.BeginScrollView(modListScrollPosition);

                var modNames = MelonPreferences.Categories
                    .Select(c => c.Identifier.Split('.')[0])
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                foreach (var modName in modNames)
                {
                    GUIStyle currentStyle = selectedMod == modName ? selectedModStyle : modListButtonStyle;
                    if (GUILayout.Button(modName, currentStyle))
                    {
                        selectedMod = modName;
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                // Right column - Preferences
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                prefsScrollPosition = GUILayout.BeginScrollView(prefsScrollPosition);

                if (!string.IsNullOrEmpty(selectedMod))
                {
                    var categories = MelonPreferences.Categories
                        .Where(c => c.Identifier.StartsWith(selectedMod))
                        .OrderBy(c => c.Identifier)
                        .ToList();

                    foreach (var category in categories)
                    {
                        GUILayout.BeginVertical(categoryBoxStyle);
                        
                        // Category header
                        GUILayout.Label(category.DisplayName, categoryStyle);
                        
                        // Add some space after the header
                        GUILayout.Space(5);

                        // Draw preferences
                        foreach (var entry in category.Entries)
                        {
                            DrawPreferenceEntry(entry);
                            GUILayout.Space(5);
                        }

                        GUILayout.EndVertical();
                        GUILayout.Space(10);
                    }
                }
                else
                {
                    GUILayout.Label("Select a mod from the list to view its settings", optionStyle);
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawPrefsSection: {ex}");
            }
        }

        private void DrawPreferenceEntry(MelonPreferences_Entry entry)
        {
            try
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Entry header with name and description
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(entry.DisplayName, (Texture)null, entry.Description), optionStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                // Entry control
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                if (entry.BoxedValue is bool boolValue)
                {
                    bool newValue = GUILayout.Toggle(boolValue, boolValue ? "Enabled" : "Disabled", buttonStyle);
                    if (newValue != boolValue)
                    {
                        entry.BoxedValue = newValue;
                        MelonPreferences.Save();
                    }
                }
                else if (entry.BoxedValue is KeyCode keyCode)
                {
                    // Handle KeyCode preferences
                    if (isCapturingKey && currentCapturingEntry == entry)
                    {
                        GUI.color = Color.yellow;
                        GUILayout.Label("Press any key...", buttonStyle);
                        GUI.color = Color.white;
                    }
                    else if (GUILayout.Button(keyCode.ToString(), buttonStyle))
                    {
                        isCapturingKey = true;
                        currentCapturingEntry = entry;
                    }
                }
                else if (entry.BoxedValue is float || entry.BoxedValue is int || entry.BoxedValue is double)
                {
                    float minValue = float.MinValue;
                    float maxValue = float.MaxValue;
                    bool hasValueRange = false;
                    bool isFloat = entry.BoxedValue is float;
                    bool isInt = entry.BoxedValue is int;
                    float step = isInt ? 1f : 0.1f;

                    // Get value range if available
                    if (entry.Validator != null && entry.Validator.GetType().IsGenericType && 
                        entry.Validator.GetType().GetGenericTypeDefinition() == typeof(ValueRange<>))
                    {
                        var validatorType = entry.Validator.GetType();
                        var minProp = validatorType.GetProperty("Min");
                        var maxProp = validatorType.GetProperty("Max");
                        
                        if (minProp != null && maxProp != null)
                        {
                            minValue = Convert.ToSingle(minProp.GetValue(entry.Validator));
                            maxValue = Convert.ToSingle(maxProp.GetValue(entry.Validator));
                            hasValueRange = true;

                            // Calculate appropriate step based on range size
                            float range_size = maxValue - minValue;
                            
                            if (range_size <= 1)
                            {
                                step = isFloat ? 0.1f : (isInt ? 1f : 0.1f);
                            }
                            else if (range_size <= 10)
                            {
                                step = isFloat ? 0.5f : (isInt ? 1f : 0.5f);
                            }
                            else if (range_size <= 100)
                            {
                                step = isFloat ? 1f : (isInt ? 1f : 1f);
                            }
                            else if (range_size <= 1000)
                            {
                                step = isFloat ? 5f : (isInt ? 10f : 5f);
                            }
                            else if (range_size <= 10000)
                            {
                                step = isFloat ? 50f : (isInt ? 50f : 50f);
                            }
                            else
                            {
                                step = isInt ? 100f : (range_size / 100f);
                            }
                            
                            if (isInt) step = Mathf.Max(1f, Mathf.Floor(step));
                        }
                    }

                    float currentValue = Convert.ToSingle(entry.BoxedValue);
                    string valueFormat = (!isInt && step < 1) ? "F2" : "F0";
                    GUILayout.Label(currentValue.ToString(valueFormat), GUILayout.Width(50));

                    if (GUILayout.Button("-", buttonStyle, GUILayout.Width(30)))
                    {
                        if (entry.BoxedValue is double)
                        {
                            double newValue = Math.Max(Convert.ToDouble(minValue), Convert.ToDouble(currentValue) - Convert.ToDouble(step));
                            if (isInt) newValue = Math.Floor(newValue);
                            SetEntryValue(entry, newValue);
                        }
                        else
                        {
                            float newValue = Mathf.Max(minValue, currentValue - step);
                            if (isInt) newValue = Mathf.Floor(newValue);
                            SetEntryValue(entry, isInt ? (int)newValue : newValue);
                        }
                    }

                    if (GUILayout.Button("+", buttonStyle, GUILayout.Width(30)))
                    {
                        if (entry.BoxedValue is double)
                        {
                            double newValue = Math.Min(Convert.ToDouble(maxValue), Convert.ToDouble(currentValue) + Convert.ToDouble(step));
                            if (isInt) newValue = Math.Floor(newValue);
                            SetEntryValue(entry, newValue);
                        }
                        else
                        {
                            float newValue = Mathf.Min(maxValue, currentValue + step);
                            if (isInt) newValue = Mathf.Floor(newValue);
                            SetEntryValue(entry, isInt ? (int)newValue : newValue);
                        }
                    }

                    // Range display
                    if (hasValueRange)
                    {
                        string rangeFormat = (!isInt && step < 1) ? "F2" : "F0";
                        GUILayout.Label($"[{minValue.ToString(rangeFormat)} - {maxValue.ToString(rangeFormat)}]", GUILayout.Width(120));
                    }
                    GUILayout.Label($"Step: {step.ToString(valueFormat)}", GUILayout.Width(70));
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DrawPreferenceEntry: {ex}");
            }
        }

        private void SetEntryValue(MelonPreferences_Entry entry, dynamic newValue)
        {
            try
            {
                // Handle type conversion based on the entry's actual type
                if (entry.BoxedValue is double)
                {
                    entry.BoxedValue = Convert.ToDouble(newValue);
                }
                else if (entry.BoxedValue is float)
                {
                    entry.BoxedValue = Convert.ToSingle(newValue);
                }
                else if (entry.BoxedValue is int)
                {
                    entry.BoxedValue = Convert.ToInt32(newValue);
                }
                else
                {
                    // For other types, try direct assignment
                    entry.BoxedValue = newValue;
                }
                
                MelonPreferences.Save();
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error setting value: {ex}");
            }
        }

        public override void OnInitializeMelon()
        {
            // Initialize mod files list
            if (Directory.Exists(modsPath))
            {
                var enabledMods = Directory.GetFiles(modsPath, "*.dll")
                    .Where(path => !path.EndsWith(".disabled")) // Exclude .dll.disabled files
                    .Select(path => (path, isEnabled: true));
                    
                var disabledMods = Directory.GetFiles(modsPath, "*.dll.disabled")
                    .Select(path => (path, isEnabled: false));
                
                modFiles = enabledMods.Concat(disabledMods)
                    .OrderBy(x => Path.GetFileName(x.path))
                    .ToList();
            }
        }

        private void RefreshModList()
        {
            if (Directory.Exists(modsPath))
            {
                var enabledMods = Directory.GetFiles(modsPath, "*.dll")
                    .Where(path => !path.EndsWith(".disabled")) // Exclude .dll.disabled files
                    .Select(path => (path, isEnabled: true));
                    
                var disabledMods = Directory.GetFiles(modsPath, "*.dll.disabled")
                    .Select(path => (path, isEnabled: false));
                
                modFiles = enabledMods.Concat(disabledMods)
                    .OrderBy(x => Path.GetFileName(x.path))
                    .ToList();
            }
        }

        public override void OnApplicationQuit()
        {
            // Clean up any remaining UI elements
            if (modSettingsButton != null)
            {
                GameObject.Destroy(modSettingsButton);
                modSettingsButton = null;
            }
            buttonInitialized = false;
        }
    }
}
