using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NodeCurves {
    public List<Keyframe> quatX;
    public List<Keyframe> quatY;
    public List<Keyframe> quatZ;
    public List<Keyframe> quatW;
    
    public List<Keyframe> posX;
    public List<Keyframe> posY;
    public List<Keyframe> posZ;

    private bool IsNearlyEqual(float a, float b, float epsilon = 0.000001f)
    {
        return Mathf.Abs(a - b) < epsilon;
    }

    private bool IsQuatNearlyEqualLast(Quaternion quat) {
        if (quatX == null || quatX.Count == 0) {
            return false;
        }
        return IsNearlyEqual(quatX.Last().value, quat.x) && IsNearlyEqual(quatY.Last().value, quat.y) && IsNearlyEqual(quatZ.Last().value, quat.z) && IsNearlyEqual(quatW.Last().value, quat.w, 0.0001f);
    }

    private bool IsPosNearlyEqualLast(Vector3 pos) {
        if (posX == null || posX.Count == 0) {
            return false;
        }
        return IsNearlyEqual(posX.Last().value, pos.x) && IsNearlyEqual(posY.Last().value, pos.y) && IsNearlyEqual(posZ.Last().value, pos.z);
    }

    public void AddQuatFrameIfNotSameAsLast(float time, Quaternion quat) {
        if (quatX == null) {
            InitForQuat();
        }
        if (!IsQuatNearlyEqualLast(quat)) {
            quatX.Add(new Keyframe(time, quat.x));
            quatY.Add(new Keyframe(time, quat.y));
            quatZ.Add(new Keyframe(time, quat.z));
            quatW.Add(new Keyframe(time, quat.w));
        }
    }

    public void AddPosFrameIfNotSameAsLast(float time, Vector3 pos) {
        if (posX == null) {
            InitForPos();
        }
        if (!IsPosNearlyEqualLast(pos)) {
            posX.Add(new Keyframe(time, pos.x));
            posY.Add(new Keyframe(time, pos.y));
            posZ.Add(new Keyframe(time, pos.z));
        }
    }

    public void InitForQuat() {
        quatX = new List<Keyframe>();
        quatY = new List<Keyframe>();
        quatZ = new List<Keyframe>();
        quatW = new List<Keyframe>();
    }

    public void InitForPos() {
        posX = new List<Keyframe>();
        posY = new List<Keyframe>();
        posZ = new List<Keyframe>();
    }
}

public class AnimData {
    public string name;
    public int frameCount;
    public Dictionary<Transform, NodeCurves> data;

    public AnimData(string name, int frameCount) {
        this.name = name;
        this.frameCount = frameCount;
        data = new Dictionary<Transform, NodeCurves>();
    }
}

public class BoneData {
    public string name;
    public Transform transform;
    public Quaternion initRotation;
    public Quaternion initRotationToRoot;
    public Vector3 initPosition;
    public Vector3 initPositionToRoot;
    public BoneData(string name, Transform transform, Quaternion initQuaternion, Vector3 initPosition, Quaternion initQuaternionToRoot, Vector3 initPositionToRoot)
    {
        this.name = name;
        this.transform = transform;
        this.initRotation = initQuaternion;
        this.initPosition = initPosition;
        this.initRotationToRoot = initQuaternionToRoot;
        this.initPositionToRoot = initPositionToRoot;
    }
}

public class AnimRetargeting
{
    public Transform sourceRoot;
    public Transform targetRoot;

    // public Dictionary<string, string> mapping;
    public List<string[]> mappingList;
    private Dictionary<string, BoneData> sourceBones;
    private Dictionary<string, BoneData> targetBones;
    private Vector3 sourceToTargetScale;
    private float sourceInitY = 0;

    public AnimRetargeting(Transform sourceRoot, Transform targetRoot, List<string[]> mapping)
    {
        this.sourceRoot = sourceRoot;
        this.targetRoot = targetRoot;
        this.mappingList = mapping;
        Init();
    }

    void Init()
    {
        sourceBones = new Dictionary<string, BoneData>();
        targetBones = new Dictionary<string, BoneData>();

        foreach (var info in mappingList) {
            var sourceBone = Util.FindBone(sourceRoot, info[0]);
            var targetBone = Util.FindBone(targetRoot, info[1]);
            if (sourceBone != null && targetBone != null) {
                var sourceBoneRotToRoot = Util.GetRotationToRoot(sourceBone, sourceRoot);
                var sourceBonePosToRoot = Util.GetPositionToRoot(sourceBone, sourceRoot);
                sourceBones.Add(info[0], new BoneData(info[0], sourceBone, sourceBone.rotation, sourceBone.position, sourceBoneRotToRoot, sourceBonePosToRoot));

                var targetBoneRotToRoot = Util.GetRotationToRoot(targetBone, targetRoot);
                var targetBonePosToRoot = Util.GetPositionToRoot(targetBone, targetRoot);
                targetBones.Add(info[1], new BoneData(info[1], targetBone, targetBone.rotation, targetBone.position, targetBoneRotToRoot, targetBonePosToRoot));
            }
        }
        var sourceBounds = Util.CalculateBounds(sourceRoot);
        var targetBounds = Util.CalculateBounds(targetRoot);

        var sourceHipPos = Util.FindBone(sourceRoot, mappingList[0][0]).position;
        var targetHipPos = Util.FindBone(targetRoot, mappingList[0][1]).position;
        Debug.Log($"sourceHipPos: {sourceHipPos} targetHipPos: {targetHipPos}");
        float heightScale = (sourceHipPos.y - sourceBounds.min.y) / (targetHipPos.y - targetBounds.min.y);
        Debug.Log($"source min y {sourceBounds.min.y} target min y {targetBounds.min.y}"); 

        sourceInitY = sourceBounds.min.y;

        // sourceToTargetScale = new Vector3(targetBounds.size.x / sourceBounds.size.x, targetBounds.size.y / sourceBounds.size.y, targetBounds.size.z / sourceBounds.size.z);
        sourceToTargetScale = new Vector3(heightScale, heightScale, heightScale);
        Debug.Log($"sourceBounds size: {sourceBounds.size} sourceBounds center: {sourceBounds.center} targetBounds size: {targetBounds.size} targetBounds center: {targetBounds.center} sourceToTargetScale: {sourceToTargetScale}");

        // AddDebugBox(sourceBounds, targetBounds);
    }

    private void AddDebugBox(Bounds sourceBounds, Bounds targetBounds) {
        var sourceBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sourceBox.transform.position = sourceBounds.center;
        sourceBox.transform.localScale = sourceBounds.size;
        sourceBox.GetComponent<MeshRenderer>().material.color = Color.red;
        var targetBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetBox.transform.position = targetBounds.center;
        targetBox.transform.localScale = targetBounds.size;
        targetBox.GetComponent<MeshRenderer>().material.color = Color.green;
    }

    public AnimationClip RetargetAnim(AnimationClip clip)
    {
        Debug.Log("RetargetAnim");
        int totalFrame = (int)Math.Floor(clip.frameRate * clip.length);
        // totalFrame = 100;
        var animData = new AnimData(clip.name, totalFrame);
        for (int i = 0; i < totalFrame; i++)
        {
            Debug.Log($"RetargetAnim frame={i}");
            float time = i / clip.frameRate;
            clip.SampleAnimation(sourceRoot.gameObject, time);
            SkeletonRetarget();
            SaveAnimationData(animData, time);
        }

        Debug.Log("RetargetAnim Done");
        return SaveAnimationDataToUnityAnim(animData);
    }

    AnimationClip SaveAnimationDataToUnityAnim(AnimData animData) {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 30;
        clip.name = animData.name;
        foreach (var pair in animData.data) {
            var node = pair.Key;
            var nodeCurves = pair.Value;
            string path = Util.GetPathTo(node, targetRoot);
            clip.SetCurve(path, typeof(Transform), "localRotation.x", new AnimationCurve(nodeCurves.quatX.ToArray()));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", new AnimationCurve(nodeCurves.quatY.ToArray()));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", new AnimationCurve(nodeCurves.quatZ.ToArray()));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", new AnimationCurve(nodeCurves.quatW.ToArray()));
            if (nodeCurves.posX != null) {
                clip.SetCurve(path, typeof(Transform), "localPosition.x", new AnimationCurve(nodeCurves.posX.ToArray()));
                clip.SetCurve(path, typeof(Transform), "localPosition.y", new AnimationCurve(nodeCurves.posY.ToArray()));
                clip.SetCurve(path, typeof(Transform), "localPosition.z", new AnimationCurve(nodeCurves.posZ.ToArray()));
            }
        }
        clip.legacy = true;
        return clip;
    }

    void SaveAnimationData(AnimData animData, float time) {
        foreach (var pair in mappingList) {
            var node = targetBones[pair[1]].transform;
            if (!animData.data.ContainsKey(node)) {
                animData.data.Add(node, new NodeCurves());
            }
            var nodeCurves = animData.data[node];
            nodeCurves.AddQuatFrameIfNotSameAsLast(time, node.localRotation);
            // record rotation
            if (pair[1] == "Root_M" || pair[1] == "Breast_L" || pair[1] == "Breast_R") {
                nodeCurves.AddPosFrameIfNotSameAsLast(time, node.localPosition);
            }
            if (pair[1] == "Shoulder_R")
                Debug.Log($"SaveAnimationData {pair[1]} {node.localRotation}");
        }
    }

    void SkeletonRetarget() {
        foreach (var pair in mappingList)
        {
            var sourceBoneData = sourceBones[pair[0]];
            var targetBoneData = targetBones[pair[1]];

            Quaternion sourceRotationToRoot = Util.GetRotationToRoot(sourceBoneData.transform, sourceRoot);

            var rotation = targetRoot.rotation;
            rotation *= sourceRotationToRoot * Quaternion.Inverse(sourceBoneData.initRotation);
            rotation *= targetBoneData.initRotation;
            targetBoneData.transform.rotation = rotation;

            if (pair[1] == "Root_M" || pair[1] == "Breast_L" || pair[1] == "Breast_R")
            {
                Vector3 sourcePosition = sourceRoot.InverseTransformPoint(sourceBoneData.transform.position);
                var diff = sourcePosition - sourceBoneData.initPositionToRoot;
                if (pair[1] == "Root_M") {
                    diff.y += sourceInitY;
                }
                diff.Scale(sourceToTargetScale);
                targetBoneData.transform.position = targetRoot.TransformPoint(diff + targetBoneData.initPositionToRoot);
            }
        }
    }
}
