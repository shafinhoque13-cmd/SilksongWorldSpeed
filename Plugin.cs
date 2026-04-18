using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 500, 600);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeed = 1.0f;

        // Optimization: Store the NPC so we don't have to search for it every frame
        private GameObject _targetNpc;
        private float _searchTimer = 0f;

        void Awake()
        {
            // Load saved position and settings
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 50);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            // Set global scale for mobile touch screens
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
            {
                // DragWindow MUST be called at the end of the window function to work with buttons
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG ME");
            }
            else
            {
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "SPRINTMASTER CONTROL");
            }
        }

        void DrawBubble(int windowID)
        {
            // We leave a small "handle" at the top for dragging, button is slightly lower
            if (GUI.Button(new Rect(10, 30, 180, 60), "OPEN MENU")) _showMenu = true;
            
            // This allows the "DRAG ME" title bar to be the touch handle
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=30>Target Speed: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " SPEED UP")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW DOWN")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(40);
            if (GUILayout.Button("CLOSE & SAVE", GUILayout.Height(80))) 
            {
                PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
                PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
                PlayerPrefs.Save();
                _showMenu = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            // 1. Search for Sprintmaster once every 2 seconds (Prevents FPS drop)
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= 2f || _targetNpc == null)
            {
                _targetNpc = GameObject.Find("Sprintmaster");
                // If direct find fails, try the common mobile name variation
                if (_targetNpc == null) _targetNpc = GameObject.Find("Sprintmaster(Clone)");
                _searchTimer = 0;
            }

            // 2. Force Speed if target is found
            if (_targetNpc != null)
            {
                // Force visual animation speed
                var anim = _targetNpc.GetComponentInChildren<Animator>(true);
                if (anim != null) anim.speed = _currentSpeed;

                // Force internal logic speed (Spine/PlayMaker hooks)
                _targetNpc.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                _targetNpc.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
