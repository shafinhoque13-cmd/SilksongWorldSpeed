using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 450);
        
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
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "TARGETED NPC CONTROL");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Sprintmaster Target: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.Space(20);
            // This specific logic is designed to find the NPC in the world data provided
            if (GUILayout.Button("ACTIVATE SPEED", GUILayout.Height(60))) { 
                SyncSpeed(); 
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(70))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void SyncSpeed()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // Directly find the NPC by its unique internal tag used in the .dat world structure
            GameObject sprintmaster = GameObject.Find("Sprintmaster");
            
            if (sprintmaster == null) {
                // Search for any object containing the name if the exact match fails
                foreach (var obj in GameObject.FindObjectsOfType<GameObject>()) {
                    if (obj.name.Contains("Sprintmaster") || obj.name.Contains("NPC_Sprint")) {
                        sprintmaster = obj;
                        break;
                    }
                }
            }

            if (sprintmaster != null) {
                // 1. Force the Spine Skeleton timeScale (Visual movement)
                sprintmaster.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                
                // 2. Force the PlayMaker Logic (Decision making speed)
                sprintmaster.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                
                // 3. Force the Physic body (Walking speed)
                var rb = sprintmaster.GetComponent<Rigidbody2D>();
                if (rb != null) {
                    sprintmaster.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
                
                // Ensure Hornet (Layer 9) is reset to 1.0 just in case
                GameObject.FindGameObjectWithTag("Player")?.SendMessage("set_timeScale", 1.0f, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SaveSettings() {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            SyncSpeed();
        }
    }
}
