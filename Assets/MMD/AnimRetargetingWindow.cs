using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AnimRetargetingWindow : EditorWindow
{
    private static string basePath = "Assets/MMD/";
    private UnityEngine.Object mirrorLifePrefabAsset, vmdFBXAsset;
    private HumanTemplate mirrorLifeHTAsset, vmdHTAsset;
    private DefaultAsset vmdAsset;

    [MenuItem("Tools/VMD动画映射", false, 10)]
    public static void ShowConverterWindow()
    {
        GetWindow(typeof(AnimRetargetingWindow));
    }

    private bool IsValidAsset<T>(T asset, string extension) where T: UnityEngine.Object
    {
        if (asset == null) return false;
        var path = AssetDatabase.GetAssetPath(asset); 
        if (string.IsNullOrEmpty(path)) return false;
        if (!path.ToLower().EndsWith(extension)) return false;
        return true;
    }

    private T AddAssetUI<T>(string label, string extension, T asset, string defaultPath) where T: UnityEngine.Object {
        if (asset == null && !string.IsNullOrEmpty(defaultPath)) {
            asset = AssetDatabase.LoadAssetAtPath<T>(defaultPath);
            if (asset == null) {
                Debug.Log($"load default asset failed! defaultPath: {defaultPath} asset: {asset}");
            }
        }

        var newAsset = (T)EditorGUILayout.ObjectField(label, asset, typeof(T), false);
        if (IsValidAsset(newAsset, extension)) {
            asset = newAsset;
        } else if (newAsset != asset) {
            EditorUtility.DisplayDialog("提示", $"{label} 文件类型错误！", "确认");
        }
        return asset;
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        GUILayout.Space(10);
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("动画映射");
        GUILayout.Space(10);

        mirrorLifePrefabAsset = AddAssetUI("镜面人生prefab", ".prefab", mirrorLifePrefabAsset, basePath + "res/mr-base-model/SK_Body_F.prefab");
        mirrorLifeHTAsset = AddAssetUI("镜面人生HT", ".ht", mirrorLifeHTAsset, basePath + "res/mr-base-model/SK_Body_F.ht");
        vmdHTAsset = AddAssetUI("VMD HT", ".ht", vmdHTAsset, basePath + "res/pmx/mmd.ht");
        vmdAsset = AddAssetUI("VMD", ".vmd", vmdAsset, "");
        vmdFBXAsset = AddAssetUI("VMD FBX", ".fbx", vmdFBXAsset, "");

        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();

        GUI.skin.label.fontSize = 12;
        if (GUILayout.Button("转换动画")) {
            var mirrorLifePrefabPath = AssetDatabase.GetAssetPath(mirrorLifePrefabAsset);
            var mirrorLifeHTPath = AssetDatabase.GetAssetPath(mirrorLifeHTAsset);
            var vmdHTPath = AssetDatabase.GetAssetPath(vmdHTAsset);
            var vmdPath = AssetDatabase.GetAssetPath(vmdAsset);
            var vmdFBXPath = AssetDatabase.GetAssetPath(vmdFBXAsset);
            var animSavePath = Path.ChangeExtension(vmdPath, "anim");
            var converter = new VMDConverter(vmdFBXPath, mirrorLifePrefabPath, vmdHTPath, mirrorLifeHTPath, vmdPath, animSavePath);
            var clip = converter.Convert();
            if (clip != null) {
                AssetDatabase.CreateAsset(clip, animSavePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        GUILayout.Space(10);
    }
}
