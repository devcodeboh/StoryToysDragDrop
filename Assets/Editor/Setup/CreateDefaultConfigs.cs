using System.IO;
using UnityEditor;
using UnityEngine;

namespace StoryToys.DragDrop.EditorTools
{
    public static class CreateDefaultConfigs
    {
        private const string ResourcesConfigDir = "Assets/Resources/Config";
        private const string DragDropSettingsPath = ResourcesConfigDir + "/DragDropSettings.asset";
        private const string OutlineSettingsPath  = ResourcesConfigDir + "/OutlineSettings.asset";
        private const string RobotPrefabPath      = "Assets/Resources/Prefabs/Robot.prefab";
        private const string TutorialStepsPath    = ResourcesConfigDir + "/TutorialSteps.asset";
        private const string TutorialStylePath    = ResourcesConfigDir + "/TutorialStyle.asset";

        [MenuItem("Tools/StoryToys/Create Default Config & Assign")]
        public static void CreateAndAssign()
        {
            EnsureDir(ResourcesConfigDir);

            // DragDrop settings
            var dds = AssetDatabase.LoadAssetAtPath<DragDropSettings>(DragDropSettingsPath);
            if (dds == null)
            {
                dds = ScriptableObject.CreateInstance<DragDropSettings>();
                dds.equipSpeed = 6f;
                dds.returnSpeed = 6f;
                dds.hitClip  = LoadClip("Assets/Audio/SFX/positive_sound.ogg");
                dds.missClip = LoadClip("Assets/Audio/SFX/negative_sound.ogg");
                AssetDatabase.CreateAsset(dds, DragDropSettingsPath);
                Debug.Log("[CreateDefaults] Created DragDropSettings at " + DragDropSettingsPath);
            }

            // Outline settings
            var os = AssetDatabase.LoadAssetAtPath<OutlineSettings>(OutlineSettingsPath);
            if (os == null)
            {
                os = ScriptableObject.CreateInstance<OutlineSettings>();
                os.outlineColor   = new Color(163f/255f, 177f/255f, 193f/255f, 1f); // #A3B1C1
                os.thicknessPx    = 3f;
                os.softness       = 0.08f;
                os.alphaThreshold = 0.5f;
                AssetDatabase.CreateAsset(os, OutlineSettingsPath);
                Debug.Log("[CreateDefaults] Created OutlineSettings at " + OutlineSettingsPath);
            }

            // Tutorial steps asset (two-step default)
            var ts = AssetDatabase.LoadAssetAtPath<TutorialSteps>(TutorialStepsPath);
            if (ts == null)
            {
                ts = ScriptableObject.CreateInstance<TutorialSteps>();
                ts.steps.Add(new TutorialStepData { message = "Drag the jacket", target = TutorialTarget.Jacket, gatePick = true, gateDropOnSlot = false });
                ts.steps.Add(new TutorialStepData { message = "Drop on the torso", target = TutorialTarget.TorsoSlot, gatePick = false, gateDropOnSlot = true });
                AssetDatabase.CreateAsset(ts, TutorialStepsPath);
                Debug.Log("[CreateDefaults] Created TutorialSteps at " + TutorialStepsPath);
            }

            // Tutorial style (empty, ready for user sprites)
            var tstyle = AssetDatabase.LoadAssetAtPath<TutorialStyle>(TutorialStylePath);
            if (tstyle == null)
            {
                tstyle = ScriptableObject.CreateInstance<TutorialStyle>();
                // Light backgrounds with black text by default
                tstyle.messageColor = new Color(0.93f, 0.93f, 0.93f, 1f);
                tstyle.skipColor    = new Color(0.95f, 0.95f, 0.95f, 1f);
                tstyle.messageTextColor = Color.white;
                tstyle.skipTextColor    = Color.white;
                tstyle.skipAnchorOffset = new Vector2(-60f, -50f);
                tstyle.hintColor        = new Color(0.95f, 0.95f, 0.95f, 1f);
                tstyle.hintAnchorOffset = new Vector2(-160f, -50f);
                AssetDatabase.CreateAsset(tstyle, TutorialStylePath);
                Debug.Log("[CreateDefaults] Created TutorialStyle at " + TutorialStylePath + ". Assign your sprites there.");
            }

            // Assign OutlineSettings to EquipSlot in Robot prefab if present
            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotPrefabPath);
            if (robotPrefab != null)
            {
                var contents = PrefabUtility.LoadPrefabContents(RobotPrefabPath);
                bool changed = false;
                var equipSlots = contents.GetComponentsInChildren<EquipSlot>(true);
                foreach (var slot in equipSlots)
                {
                    if (slot != null)
                    {
                        if (slot.GetType().GetField("outlineSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public) != null)
                        {
                            slot.GetType().GetField("outlineSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                .SetValue(slot, os);
                            changed = true;
                        }
                        // no public API; reflection path above covers the private field
                    }
                }

                // Ensure BlinkController on root with two-eye setup
                var rootSR = contents.GetComponent<SpriteRenderer>();
                if (rootSR != null)
                {
                    var blink = contents.GetComponent<BlinkController>();
                    if (blink == null)
                    {
                        blink = contents.AddComponent<BlinkController>();
                        var b = rootSR.bounds;
                        var xOff = b.size.x * 0.18f;
                        var yOff = b.size.y * 0.15f;
                        var aL = new GameObject("EyeAnchor_L").transform; aL.SetParent(contents.transform, false); aL.localPosition = new Vector3(-xOff, yOff, 0f);
                        var aR = new GameObject("EyeAnchor_R").transform; aR.SetParent(contents.transform, false); aR.localPosition = new Vector3( xOff, yOff, 0f);
                        blink.eyeAnchors = new[] { aL, aR };
                        blink.eyeOffsets = new[] { Vector2.zero, Vector2.zero };
                        var w = b.size.x * 0.17f; // per-eye width ~17% от ширины
                        blink.closedSize = new Vector2(w, 0.12f);
                        blink.openHeight = 0.02f;
                        blink.intervalMin = 2.0f;
                        blink.intervalMax = 6.0f;
                        blink.sortingOrderOffset = 20;
                        changed = true;
                    }
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, RobotPrefabPath);
                    Debug.Log("[CreateDefaults] Assigned OutlineSettings in Robot.prefab");
                }
                PrefabUtility.UnloadPrefabContents(contents);
            }
            else
            {
                Debug.LogWarning("[CreateDefaults] Robot.prefab not found at " + RobotPrefabPath + ". Skipping assignment.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private static AudioClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
