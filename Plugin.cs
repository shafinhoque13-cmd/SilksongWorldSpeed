using BepInEx;
using UnityEngine;
using System;
using System.Reflection;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 400);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC SPEED MOD");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MENU")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Target Multiplier: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.Space(20);
            // This button triggers a single, high-intensity search to find Sprintmaster
            if (GUILayout.Button("FORCE UPDATE SPRINTMASTER", GUILayout.Height(60))) { 
                ForceNpcUpdate(); 
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(70))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void ForceNpcUpdate()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // We look for every object. This is only done when you click the button (FPS Safe).
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var obj in all)
            {
                if (obj == null) continue;
                string lowerName = obj.name.ToLower();

                // Check for name variations found in the .dat file
                if (lowerName.Contains("sprintmaster") || lowerName.Contains("sprint_master"))
                {
                    // 1. Direct Animator Speed
                    var anim = obj.GetComponentInChildren<Animator>(true);
                    if (anim != null) anim.speed = _currentSpeedMult;

                    // 2. Spine Skeleton Speed (Crucial for 2D NPCs)
                    // We use Reflection to find the 'timeScale' field in case the Spine DLL is hidden
                    foreach (var component in obj.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null) continue;
                        Type type = component.GetType();
                        
                        // Look for SkeletonAnimation or SkeletonRenderer
                        if (type.Name.Contains("Skeleton"))
                        {
                            FieldInfo timeScaleField = type.GetField("timeScale");
                            if (timeScaleField != null) timeScaleField.SetValue(component, _currentSpeedMult);
                            
                            PropertyInfo timeScaleProp = type.GetProperty("timeScale");
                            if (timeScaleProp != null) timeScaleProp.SetValue(component, _currentSpeedMult, null);
                        }
                    }

                    // 3. FSM Speed (Decision making)
                    obj.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void SaveSettings() {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            ForceNpcUpdate();
        }
    }
}
