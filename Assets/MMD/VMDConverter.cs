using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class VMDConverter
{
    private string basePath = "Assets/MMD/";
    private string mmdFBXPath;
    private string mirrorLifePrefabPath;
    private string mmdHTFilePath;
    private string mirrorLifeHTFilePath;
    private string vmdFilePath;

    private Transform sourceModel;
    private Transform targetModel;
    private Dictionary<string, Transform> sourceBoneMap;
    private Dictionary<string, Transform> targetBoneMap;


    public VMDConverter(string mmdFBXPath, string mirrorLifePrefabPath, string mmdHTFilePath, string mirrorLifeHTFilePath, string vmdFilePath, string animSavePath) {
        this.mmdFBXPath = mmdFBXPath;
        this.mirrorLifePrefabPath = mirrorLifePrefabPath;
        this.mmdHTFilePath = mmdHTFilePath;
        this.mirrorLifeHTFilePath = mirrorLifeHTFilePath;
        this.vmdFilePath = vmdFilePath;
    }

    public AnimationClip Convert() {
        LoadConvertScene();
        sourceModel = Util.LoadPrefab(mmdFBXPath);
        targetModel = Util.LoadPrefab(mirrorLifePrefabPath);
        sourceModel.localPosition = new Vector3(1, 0, 0);
        targetModel.localPosition = new Vector3(-1, 0, 0);

        sourceBoneMap = GuessBoneMapByHT(sourceModel, mmdHTFilePath);
        MakeMMDTPose(sourceModel, sourceBoneMap);

        targetBoneMap = GuessBoneMapByHT(targetModel, mirrorLifeHTFilePath);

        var clip = Util.LoadAnimToModel(sourceModel, mmdFBXPath);
        // var mapping = Util.GetMapping(sourceBoneMap, targetBoneMap);
        var mappingList = Util.GetListMapping(sourceBoneMap, targetBoneMap);
        for (int i = 0; i < mappingList.Count; i++)
        {
            Debug.Log($"mappingList[{i}]: {mappingList[i][0]} => {mappingList[i][1]}");
        }

        var animRetarting = new AnimRetargeting(sourceModel, targetModel, mappingList);
        var targetClip = animRetarting.RetargetAnim(clip);
        AddBSCurves(targetClip);

        return targetClip;
    }

    private void AddBSCurves(AnimationClip clip) {
        var bsClip = VMDTools.ConvertVMDBSAnim(vmdFilePath);
        var bindings = AnimationUtility.GetCurveBindings(bsClip);
        foreach (var binding in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(bsClip, binding);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }


    private void LoadConvertScene() {
        var scenePath = basePath + "res/scene/ConvertScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var rootObjects = scene.GetRootGameObjects();
        foreach (var o in rootObjects)
        {
            Debug.Log($"o.name: {o.name}");
            if (o.name != "Main Camera" && o.name != "Directional Light")
            {
                UnityEngine.Object.DestroyImmediate(o);
            }
        }
    }

    private Dictionary<string, Transform> GuessBoneMapByHT(Transform model, string htFilePath)
    {
        var ht = HTFileLoader.Load(htFilePath);
        Debug.Log($"name: {ht.Name} ht.Bones.Count: {ht.Bones.Count} {ht.Bones}");
        var boneMap = new Dictionary<string, Transform>();
        foreach (var info in ht.Bones)
        {
            var key = info.Key;
            var value = info.Value;
            var bone = Util.FindNodeByNameEndsWidth(model, value);
            if (bone == null) {
                if (value.Contains("|")) {
                    var list = value.Split('|');
                    foreach (var v in list) {
                        bone = Util.FindNodeByNameEndsWidth(model, v);
                        if (bone != null) {
                            break;
                        }
                    }
                }
                if (bone == null) {
                    Debug.Log($"bone is null: {value}");
                    continue;
                }
            }
            boneMap.Add(key, bone);
        }
        return boneMap;
    }

    private void MakeMMDAPose(Transform model, Dictionary<string, Transform> boneMap)
    {
        foreach (var info in boneMap)
        {
            var bone = info.Value;
            bone.localRotation = Quaternion.identity;
            if (info.Key == "Hips") {
                bone.localPosition = new Vector3(0, 1.08f, 0);
            }
        }
    }

    private void MakeMMDTPose(Transform model, Dictionary<string, Transform> boneMap)
    {
        MakeMMDAPose(model, boneMap);
        boneMap["LeftUpperArm"].localRotation = Quaternion.Euler(0, 0, -36);
        boneMap["RightUpperArm"].localRotation = Quaternion.Euler(0, 0, 36);
    }
}