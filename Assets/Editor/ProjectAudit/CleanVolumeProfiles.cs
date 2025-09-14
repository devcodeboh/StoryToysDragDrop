using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace StoryToys.DragDrop.EditorTools
{
    public static class CleanVolumeProfiles
    {
        [MenuItem("Tools/Project Audit/Clean Volume Profiles (remove null overrides)")]
        public static void CleanAll()
        {
            var guids = AssetDatabase.FindAssets("t:VolumeProfile");
            int cleaned = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (vp == null) continue;

                var so = new SerializedObject(vp);
                var comps = so.FindProperty("components");
                if (comps == null || !comps.isArray) continue;

                bool changed = false;
                for (int i = comps.arraySize - 1; i >= 0; i--)
                {
                    var elem = comps.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue == null)
                    {
                        comps.DeleteArrayElementAtIndex(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(vp);
                    cleaned++;
                    Debug.Log($"[CleanVolumeProfiles] Removed null overrides from {path}");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[CleanVolumeProfiles] Completed. Profiles cleaned: {cleaned}");
        }

        [MenuItem("Tools/Project Audit/Create Empty Volume Profile (Settings/URP)")]
        public static void CreateEmpty()
        {
            const string dir = "Assets/Settings/URP";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Settings", "URP");

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/EmptyVolumeProfile.asset");
            var vp = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(vp, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = vp;
            Debug.Log($"[CleanVolumeProfiles] Created: {assetPath}");
        }
    }
}

