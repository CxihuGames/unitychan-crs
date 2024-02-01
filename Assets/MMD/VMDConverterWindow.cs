using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class VMDConverterWindow : EditorWindow
{
    private static string basePath = "Assets/MMD/";
    private DefaultAsset vmdAsset, modelAsset;

    [MenuItem("Tools/VMD转换", false, 1)]
    public static void ShowConverterWindow()
    {
        GetWindow(typeof(VMDConverterWindow));
    }
    private bool IsValidAsset(DefaultAsset asset, string extension)
    {
        if (asset == null) return false;
        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path)) return false;
        if (!path.ToLower().EndsWith(extension)) return false;
        return true;
    }
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        GUILayout.Space(10);
        GUI.skin.label.fontSize = 24;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("VMD动画转换");
        GUILayout.Space(10);
        if (modelAsset == null) {
            modelAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(basePath + "res/pmx/model.pmx");
            Debug.Log($"modelAsset: {modelAsset}");
        }
        var newModelAsset = (DefaultAsset)EditorGUILayout.ObjectField("PMX/PMD文件", modelAsset, typeof(DefaultAsset), true);
        var newVmdAsset = (DefaultAsset)EditorGUILayout.ObjectField("VMD文件", vmdAsset, typeof(DefaultAsset), true);
        if (IsValidAsset(newModelAsset, ".pmx") || IsValidAsset(newModelAsset, ".pmd")) {
            modelAsset = newModelAsset;
        } else if (newModelAsset != modelAsset) {
            EditorUtility.DisplayDialog("提示", "本工具仅支持pmx/pmd文件转换", "确认");
            return;
        }
        if (IsValidAsset(newVmdAsset, ".vmd")) {
            vmdAsset = newVmdAsset;
        } else if (newVmdAsset != vmdAsset) {
            EditorUtility.DisplayDialog("提示", "本工具仅支持vmd文件转换", "确认");
            return;
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUI.skin.label.fontSize = 12;
        if (GUILayout.Button("转换为FBX")) {
            var fbxPath = Convert(AssetDatabase.GetAssetPath(vmdAsset), AssetDatabase.GetAssetPath(modelAsset));
            if (!string.IsNullOrEmpty(fbxPath)) {
                EditorUtility.DisplayDialog("转换成功", "FBX转换成功！", "确认");
            }
        }
        GUILayout.Space(10);
    
        if (GUILayout.Button("转换为FBX并重定向动画")) {
            var vmdFBXPath = Convert(AssetDatabase.GetAssetPath(vmdAsset), AssetDatabase.GetAssetPath(modelAsset));
            if (string.IsNullOrEmpty(vmdFBXPath)) {
                EditorUtility.DisplayDialog("错误", "转换过程中出现错误，无法继续！请检查PMX和VMD文件是否有效！", "确认");
                return;
            }

            var mirrorLifePrefabPath = basePath + "res/mr-base-model/SK_Body_F.prefab";
            var mirrorLifeHTPath = basePath + "res/mr-base-model/SK_Body_F.ht";
            var vmdHTPath = basePath + "res/pmx/mmd.ht";
            var vmdPath = AssetDatabase.GetAssetPath(vmdAsset);
            var animSavePath = Path.ChangeExtension(vmdPath, "anim");
            var converter = new VMDConverter(vmdFBXPath, mirrorLifePrefabPath, vmdHTPath, mirrorLifeHTPath, vmdPath, animSavePath);
            var clip = converter.Convert();
            if (clip != null) {
                AssetDatabase.CreateAsset(clip, animSavePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("转换成功", "动画转换成功！", "确认");
            }
        }
    }

    private static string CallPMX2FBX(string exePath, string pmxPath, string vmdPath) {
        // 执行
        Debug.Log(exePath);
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = exePath;
        process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", pmxPath, string.IsNullOrEmpty(vmdPath) ? "" : vmdPath);
        process.StartInfo.UseShellExecute = true;
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            if (process.ExitCode > 0)
                throw new Exception("PMX2FBX ExitCode：" + process.ExitCode);
            else
                Debug.Log("用户手动结束");
            return "";
        }
        return pmxPath.Replace(".pmx", ".fbx");
    }

    public static string Convert(string vmdPath, string modelPath)
    {
        vmdPath = vmdPath.Replace("\\", "/");
        var originVMDFolder = Path.GetDirectoryName(vmdPath);
        Debug.Log("开始转换VMD文件...");
        var vmdName = Path.GetFileNameWithoutExtension(vmdPath);
        var dir = Environment.CurrentDirectory + "/";
        var tempDir = basePath + "res/temp/";
        var exePath = dir + basePath + "PMX2FBX/pmx2fbx.exe";
        try
        {
            // 准备Temp目录
            ClearTempFile();
            Directory.CreateDirectory(tempDir);
            File.Copy(vmdPath, tempDir + "0.vmd");
            File.Copy(modelPath, tempDir + "0" + Path.GetExtension(modelPath));
            vmdPath = dir + tempDir + "0.vmd";
            modelPath = dir + tempDir + "0" + Path.GetExtension(modelPath);

            var fbxPath = CallPMX2FBX(exePath, modelPath, vmdPath);
            var fbxName = vmdName + ".fbx";
            var fbxResPath = Path.Combine(originVMDFolder, fbxName);
            File.Copy(fbxPath, fbxResPath);
            ClearTempFile();
            // 重新加载资源
            AssetDatabase.Refresh();
            return fbxResPath;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("错误", "转换过程中出现错误，无法继续！请检查PMX和VMD文件是否有效！", "确认");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        return "";
    }
    private static void ClearTempFile()
    {
        var tempDir = basePath + "res/temp";
        try {
            Directory.Delete(tempDir, true);
            File.Delete(tempDir + ".meta");
        }
        catch (Exception) { }
    }

    [MenuItem("Assets/MMD/Export VMD Camera To Anim")]
    public static void ExportCameraVmdToAnim()
    {
        var selected = Selection.activeObject;
        string selectPath = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrEmpty(selectPath) && selectPath.EndsWith(".vmd"))
        {
            var clip = VMDTools.ConvertVMDCameraAnim(selectPath);
            if (clip != null)
            {
                var savePath = Path.ChangeExtension(selectPath, "anim");
                AssetDatabase.CreateAsset(clip, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        else
        {
            Debug.LogError("没有选中文件或文件夹");
        }
    }

    [MenuItem("Assets/MMD/Export VMD morph To Anim")]
    public static void ExportBSVmdToAnim()
    {
        var selected = Selection.activeObject;
        string selectPath = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrEmpty(selectPath) && selectPath.EndsWith(".vmd"))
        {
            var clip = VMDTools.ConvertVMDBSAnim(selectPath);
            if (clip != null)
            {
                var savePath = Path.ChangeExtension(selectPath, "bs.anim");
                AssetDatabase.CreateAsset(clip, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        else
        {
            Debug.LogError("没有选中文件或文件夹");
        }
    }

    [MenuItem("Assets/MMD/VMD转换为镜面人生动画")]
    public static void ConvertVMDToMRAnim()
    {
        var selected = Selection.activeObject;
        string selectPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(selectPath) || !selectPath.EndsWith(".vmd"))
        {
            return;
        }
        var vmdPath = selectPath;
        var vmdFBXPath = Convert(vmdPath, basePath + "res/pmx/miku.pmx");
        if (string.IsNullOrEmpty(vmdFBXPath)) {
            EditorUtility.DisplayDialog("错误", "转换过程中出现错误，无法继续！请检查VMD文件是否有效！", "确认");
            return;
        }

        var mirrorLifePrefabPath = basePath + "res/mr-base-model/SK_Body_F.prefab";
        var mirrorLifeHTPath = basePath + "res/mr-base-model/SK_Body_F.ht";
        var vmdHTPath = basePath + "res/pmx/mmd.ht";
        var animSavePath = Path.ChangeExtension(vmdPath, "anim");
        var converter = new VMDConverter(vmdFBXPath, mirrorLifePrefabPath, vmdHTPath, mirrorLifeHTPath, vmdPath, animSavePath);
        var clip = converter.Convert();
        if (clip != null) {
            AssetDatabase.CreateAsset(clip, animSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("转换成功", "动画转换成功！", "确认");
        } else {
            EditorUtility.DisplayDialog("错误", "转换过程中出现错误！", "确认");
        }
    }
}
