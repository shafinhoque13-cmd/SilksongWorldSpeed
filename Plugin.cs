using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.9.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private bool _isFrozen = false;
        private float _speedMult = 1.0f;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 600, 600);
        
        private Dictionary<int, Vector3> _posLock = new Dictionary<int, Vector3>();

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "MOD");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "CAVE CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(10, 35, 180, 55), "OPEN")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            string sceneName = SceneManager.GetActiveScene().name;
            GUILayout.Label($"Current Area: <color=yellow>{sceneName}</color>");

            GUILayout.Space(20);
            if (GUILayout.Button(_isFrozen ? "RELEASE NPC" : "FREEZE NPC", GUILayout.Height(100)))
            {
                _isFrozen = !_isFrozen;
                if (!_isFrozen) _posLock.Clear();
            }

            GUILayout.Space(10);
            GUILayout.Label("Or Slow Motion:");
            _speedMult = GUILayout.HorizontalSlider(_speedMult, 0.01f, 1.0f);
            GUILayout.Label($"Speed: {(_speedMult * 100):F0}%");

            if (GUILayout.Button("CLOSE", GUILayout.Height(60))) { _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void LateUpdate()
        {
            // Only run logic if we are in the Sprintmaster Cave or the NPC is found
            string scene = SceneManager.GetActiveScene().name;
            if (!scene.Contains("Sprintmaster") && !scene.Contains("Cave")) return;

            GameObject[] objs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in objs)
            {
                if (obj == null) continue;

                // Targeting Layer 19 (Runner) or objects named Sprintmaster
                if (obj.layer == 19 || obj.name.ToLower().Contains("sprintmaster"))
                {
                    // 1. FREEZE LOGIC
                    if (_isFrozen)
                    {
                        int id = obj.GetInstanceID();
                        if (!_posLock.ContainsKey(id)) _posLock[id] = obj.transform.position;
                        obj.transform.position = _posLock[id];
                    }

                    // 2. SPEED LOGIC (Works even if not frozen)
                    float targetSpeed = _isFrozen ? 0f : _speedMult;
                    
                    // Force the Animator
                    Animator[] anims = obj.GetComponentsInChildren<Animator>(true);
                    foreach(var a in anims) { a.speed = targetSpeed; }

                    // Force the TimeScale/Speed via messages
                    obj.SendMessage("set_timeScale", targetSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetSpeed", targetSpeed, SendMessageOptions.DontRequireReceiver);
                    
                    // Kill Velocity
                    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null && (_isFrozen || _speedMult < 1.0f))
                    {
                        rb.linearVelocity *= targetSpeed;
                    }
                }
            }
        }
    }
}
