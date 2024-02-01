using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Util
{
    public static string GetPathTo(Transform node, Transform root)
    {
        string path = node.name;
        while (node.parent != root)
        {
            node = node.parent;
            path = node.name + "/" + path;
        }
        return path;
    }

    public static Transform FindNodeByNameEndsWidth(Transform transform, string name)
    {
        if (transform.name.EndsWith(name))
        {
            return transform;
        }

        foreach (Transform child in transform)
        {
            Transform result = FindNodeByNameEndsWidth(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    // 递归查找
    public static Transform FindBone(Transform parent, string name)
    {
        if (!parent)
        {
            return null;
        }

        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindBone(parent.GetChild(i), name);
            if (result)
            {
                return result;
            }
        }

        return null;
    }


    public static Quaternion GetRotationToRoot(Transform bone, Transform root)
    {
        return Quaternion.Inverse(root.rotation) * bone.rotation; // bone to root
    }

    public static Vector3 GetPositionToRoot(Transform bone, Transform root)
    {
        return root.InverseTransformPoint(bone.position);
    }


    public static Bounds CalculateBounds(Transform node)
    {
        Bounds bounds = new Bounds();

        foreach (Renderer renderer in node.GetComponentsInChildren<Renderer>())
        {
            Bounds curBounds;
            if (renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                Mesh mesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = node.TransformPoint(vertices[i]);
                }
                mesh.vertices = vertices;
                mesh.RecalculateBounds();

                curBounds = mesh.bounds;
            }
            else
            {
                curBounds = renderer.bounds;
            }

            if (bounds.size == Vector3.zero)
            {
                bounds = curBounds;
            }
            else
            {
                bounds.Encapsulate(curBounds);
            }
        }

        return bounds;
    }

    public static Dictionary<string, string> GetMapping(Dictionary<string, Transform> sourceMap, Dictionary<string, Transform> targetMap)
    {
        var mapping = new Dictionary<string, string>();
        foreach (var source in sourceMap)
        {
            var key = source.Key;
            var sourceBone = source.Value;
            var targetBone = targetMap[key];
            mapping.Add(sourceBone.name, targetBone.name);
        }
        return mapping;
    }

    public static List<string[]> GetListMapping(Dictionary<string, Transform> sourceMap, Dictionary<string, Transform> targetMap)
    {
        var mapping = new List<string[]>();
        var mappingKeys = new string[]{
            "Hips",
            "Spine",
            "Spine1",
            "Chest",
            "UpperChest",
            "Neck",
            "Head",
            "LeftEye",
            "RightEye",
            "Jaw",
            "LeftShoulder",
            "LeftUpperArm",
            "LeftLowerArm",
            "LeftHand",
            "RightShoulder",
            "RightUpperArm",
            "RightLowerArm",
            "RightHand",
            "LeftUpperLeg",
            "LeftLowerLeg",
            "LeftFoot",
            "LeftToe",
            "RightUpperLeg",
            "RightLowerLeg",
            "RightFoot",
            "RightToe",
            "Left Thumb Proximal",
            "Left Thumb Intermediate",
            "Left Thumb Distal",
            "Left Index Proximal",
            "Left Index Intermediate",
            "Left Index Distal",
            "Left Middle Proximal",
            "Left Middle Intermediate",
            "Left Middle Distal",
            "Left Ring Proximal",
            "Left Ring Intermediate",
            "Left Ring Distal",
            "Left Little Proximal",
            "Left Little Intermediate",
            "Left Little Distal",
            "Right Thumb Proximal",
            "Right Thumb Intermediate",
            "Right Thumb Distal",
            "Right Index Proximal",
            "Right Index Intermediate",
            "Right Index Distal",
            "Right Middle Proximal",
            "Right Middle Intermediate",
            "Right Middle Distal",
            "Right Ring Proximal",
            "Right Ring Intermediate",
            "Right Ring Distal",
            "Right Little Proximal",
            "Right Little Intermediate",
            "Right Little Distal",
            // "LeftBreast",
            // "RightBreast",
        };
        
        foreach(var key in mappingKeys)
        {
            if (sourceMap.ContainsKey(key) && targetMap.ContainsKey(key))
            {
                var sourceBone = sourceMap[key];
                var targetBone = targetMap[key];
                mapping.Add(new string[]{sourceBone.name, targetBone.name});
            }
        }
        return mapping;
    }

    public static Transform LoadPrefab(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var prefabInstance = Object.Instantiate(prefab);
        return prefabInstance.transform;
    }

    public static AnimationClip LoadAnimToModel(Transform model, string animFBXPath)
    {
        var clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(animFBXPath, typeof(AnimationClip));
        clip.legacy = true;
        var anim = model.GetComponent<Animation>();
        if (anim == null)
        {
            anim = model.gameObject.AddComponent<Animation>();
        }
        anim.AddClip(clip, clip.name);
        // clip.SampleAnimation(model.gameObject, 10);
        return clip;
    }
}