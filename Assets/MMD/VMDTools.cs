using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MMD.VMD;
using System;

public class VMDTools {
    public static AnimationClip ConvertVMDBSAnim(string vmdPath) {
        var vmd = VMDLoaderScript.Import(vmdPath);
        var clip = new AnimationClip();
        clip.frameRate = 30;
        clip.name = vmd.name;

        const float tick_time = 1f / 30f;

        var leftEyeFrames = new List<Keyframe>();
        var rightEyeFrames = new List<Keyframe>();
        var mouthFrames = new List<Keyframe>();

        var morphList = vmd.morph_list;
        Debug.Log($"morph_list.morph_count: {morphList.morph_count}");
        var eyeList = morphList.morph["まばたき"];
        if (eyeList != null) {
            for (int i = 0; i < eyeList.Count; i++) {
                var frame = eyeList[i];
                var time = frame.frame_no * tick_time;
                var leftEyeFrame = new Keyframe(time, frame.weight * 100);
                var rightEyeFrame = new Keyframe(time, frame.weight * 100);
                leftEyeFrames.Add(leftEyeFrame);
                rightEyeFrames.Add(rightEyeFrame);
            }
            clip.SetCurve("HEAD", typeof(SkinnedMeshRenderer), "blendShape.EyeBlinkLeft", new AnimationCurve(leftEyeFrames.ToArray()));
            clip.SetCurve("HEAD", typeof(SkinnedMeshRenderer), "blendShape.EyeBlinkRight", new AnimationCurve(rightEyeFrames.ToArray()));
        }
        
        var mouthMorphList = new List<VMDFormat.VMDMorphData>[5];
        var mouthList = new List<VMDFormat.VMDMorphData>();

        AppendMorphRange(mouthList, morphList, "あ", 0.8f);
        AppendMorphRange(mouthList, morphList, "い", 0.2f);
        AppendMorphRange(mouthList, morphList, "う", 0.1f);
        AppendMorphRange(mouthList, morphList, "え", 0.4f);
        AppendMorphRange(mouthList, morphList, "お", 1f);
        AppendMorphRange(mouthList, morphList, "□", 1f);
        AppendMorphRange(mouthList, morphList, "ワ", 1f);
        
        mouthList.Sort((x,y)=>(int)x.frame_no - (int)y.frame_no);
        
        VMDFormat.VMDMorphData lastOpenFrame = null;
        for (int i = 0; i < mouthList.Count; i++) {
            var frame = mouthList[i];
            if (lastOpenFrame != null) {
                if ((frame.name == "い" || frame.name == "う") && frame.weight < lastOpenFrame.weight) {
                    Debug.Log($"frame.name: {frame.name} frame.weight: {frame.weight} lastOpenFrame.name: {lastOpenFrame.name} lastOpenFrame.weight: {lastOpenFrame.weight}");
                    continue;
                }
            }
            var time = frame.frame_no * tick_time;
            var mouthFrame = new Keyframe(time, frame.weight * 100);
            mouthFrames.Add(mouthFrame);
            if (frame.name != "い" && frame.name != "う") {
                lastOpenFrame = frame;
            } else {
                lastOpenFrame = null;
            }
        }

        clip.SetCurve("HEAD", typeof(SkinnedMeshRenderer), "blendShape.JawOpen", new AnimationCurve(mouthFrames.ToArray()));
        return clip;
    }

    private static void AppendMorphRange(List<VMDFormat.VMDMorphData> list, VMDFormat.VMDMorphList vmdMorphList, string key, float scale) {
        if (!vmdMorphList.morph.ContainsKey(key)) {
            return;
        }
        var subList = vmdMorphList.morph[key];
        for (int i = 0; i < subList.Count; i++) {
            var frame = subList[i];
            var newFrame = new VMDFormat.VMDMorphData();
            newFrame.name = frame.name;
            newFrame.frame_no = frame.frame_no;
            newFrame.weight = frame.weight * scale;
            list.Add(newFrame);
        }
    }
    public static AnimationClip ConvertVMDCameraAnim(string vmdPath) {
        var vmd = VMDLoaderScript.Import(vmdPath);
        const float tick_time = 1f / 30f;
        const float mmd4unity_unit = 0.08f;
        var clip = new AnimationClip();
        clip.frameRate = 30;
        clip.name = vmd.name;
        Debug.Log($"vmd.camera_list.camera_count: {vmd.camera_list.camera_count}");

        Keyframe[] posX_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] posY_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] posZ_keyframes = new Keyframe[vmd.camera_list.camera_count];

        Keyframe[] rotX_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] rotY_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] rotZ_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] rotW_keyframes = new Keyframe[vmd.camera_list.camera_count];

        Keyframe[] fov_keyframes = new Keyframe[vmd.camera_list.camera_count];

        Keyframe[] dis_x_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] dis_y_keyframes = new Keyframe[vmd.camera_list.camera_count];
        Keyframe[] dis_z_keyframes = new Keyframe[vmd.camera_list.camera_count];
        
        for (int i = 0; i < vmd.camera_list.camera_count; i++)
        {
            var cameraData = vmd.camera_list.camera[i];

            var quat = Quaternion.Euler(new Vector3(
                                    cameraData.rotation.x * Mathf.Rad2Deg,
                                    -cameraData.rotation.y * Mathf.Rad2Deg,
                                    cameraData.rotation.z * Mathf.Rad2Deg));

            float frameTime = cameraData.frame_no * tick_time;
            posX_keyframes[i] = new Keyframe(frameTime, -cameraData.location.x * mmd4unity_unit);
            posY_keyframes[i] = new Keyframe(frameTime, cameraData.location.y * mmd4unity_unit);
            posZ_keyframes[i] = new Keyframe(frameTime, -cameraData.location.z * mmd4unity_unit);

            //做动画时最好用原值,localEulerAngles取值后将角度设置为绝对值,在做补间曲线会出问题
            rotX_keyframes[i] = new Keyframe(frameTime, quat.x);
            rotY_keyframes[i] = new Keyframe(frameTime, quat.y);
            rotZ_keyframes[i] = new Keyframe(frameTime, quat.z);
            rotW_keyframes[i] = new Keyframe(frameTime, quat.w);

            //视角fov
            fov_keyframes[i] = new Keyframe(frameTime, cameraData.viewing_angle);

            //摄像机距离
            dis_x_keyframes[i] = new Keyframe(frameTime, 0);
            dis_y_keyframes[i] = new Keyframe(frameTime, 0);
            dis_z_keyframes[i] = new Keyframe(frameTime, -cameraData.length * mmd4unity_unit);
        }

        //UnityEngine.Object.DestroyImmediate(cameraWorldObj);

        //NOTE:这里"距离"已经与position融合了,所以没法做补间
        AnimationCurve posX_curve = ToAnimationCurveWithTangentMode(1, AnimationUtility.TangentMode.Free, posX_keyframes, vmd.camera_list);
        AnimationCurve posY_curve = ToAnimationCurveWithTangentMode(2, AnimationUtility.TangentMode.Free, posY_keyframes, vmd.camera_list);
        AnimationCurve posZ_curve = ToAnimationCurveWithTangentMode(3, AnimationUtility.TangentMode.Free, posZ_keyframes, vmd.camera_list);
        AnimationCurve rotX_curve = ToAnimationCurveWithTangentMode(4, AnimationUtility.TangentMode.Free, rotX_keyframes, vmd.camera_list);
        AnimationCurve rotY_curve = ToAnimationCurveWithTangentMode(4, AnimationUtility.TangentMode.Free, rotY_keyframes, vmd.camera_list);
        AnimationCurve rotZ_curve = ToAnimationCurveWithTangentMode(4, AnimationUtility.TangentMode.Free, rotZ_keyframes, vmd.camera_list);
        AnimationCurve rotW_curve = ToAnimationCurveWithTangentMode(4, AnimationUtility.TangentMode.Free, rotW_keyframes, vmd.camera_list);
        
        AnimationCurve dis_x_curve = ToAnimationCurveWithTangentMode(1, AnimationUtility.TangentMode.Free, dis_x_keyframes, vmd.camera_list);
        AnimationCurve dis_y_curve = ToAnimationCurveWithTangentMode(2, AnimationUtility.TangentMode.Free, dis_y_keyframes, vmd.camera_list);
        AnimationCurve dis_z_curve = ToAnimationCurveWithTangentMode(3, AnimationUtility.TangentMode.Free, dis_z_keyframes, vmd.camera_list);

        AnimationCurve fov_curve = ToAnimationCurveWithTangentMode(6, AnimationUtility.TangentMode.Free, fov_keyframes, vmd.camera_list);

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), posX_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), posY_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.z"), posZ_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.x"), rotX_curve);   //采用欧拉角插值方式
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.y"), rotY_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.z"), rotZ_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalRotation.w"), rotW_curve);

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance", typeof(Transform), "m_LocalPosition.x"), dis_x_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance", typeof(Transform), "m_LocalPosition.y"), dis_y_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance", typeof(Transform), "m_LocalPosition.z"), dis_z_curve);

        // AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance/Camera", typeof(Camera), "field of view"), fov_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance/VirtualCameraFOV", typeof(Transform), "m_LocalPosition.x"), fov_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance/VirtualCameraFOV", typeof(Transform), "m_LocalPosition.y"), fov_curve);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Distance/VirtualCameraFOV", typeof(Transform), "m_LocalPosition.z"), fov_curve);
        return clip;
    }

    private static AnimationCurve ToAnimationCurveWithTangentMode(int type, AnimationUtility.TangentMode mode, Keyframe[] keyframes, VMDFormat.CameraList cameraList)
    {
        if (mode == AnimationUtility.TangentMode.Free)
        {
            for (int i = 0; i < keyframes.Length; i++)
            {
                SetKeyfreamTweenCurve(i, type, keyframes, cameraList);
            }
        }

        var newKeyFrames = OptimizedCurves(type, keyframes, cameraList);

        AnimationCurve curve = new AnimationCurve(newKeyFrames);
        for (int i = 0; i < curve.keys.Length; i++)
        {
            if (mode == AnimationUtility.TangentMode.Free)
                AnimationUtility.SetKeyBroken(curve, i, true);

            AnimationUtility.SetKeyLeftTangentMode(curve, i, mode);
            AnimationUtility.SetKeyRightTangentMode(curve, i, mode);

        }
        for(int j = 0; j < keyframes.Length; j++)
        {
            if(j<keyframes.Length - 1)
            {
                int StartFrame = (int)(keyframes[j].time * 30f);
                int EndFrame = (int)(keyframes[j+1].time * 30f);

                if(EndFrame == StartFrame+1)
                {
                    //Debug.Log(StartFrame);
                    AnimationUtility.SetKeyRightTangentMode(curve, j, AnimationUtility.TangentMode.Constant);
                }
            }
        }
        return curve;
    }

    private static void SetKeyfreamTweenCurve(int index, int type, Keyframe[] keyframes, VMDFormat.CameraList cameraList)
    {
        if (index <= 0)
            return;

        VMDFormat.CameraData curCameraData = cameraList.camera[index];
        VMDFormat.CameraData lastCameraData = cameraList.camera[index - 1];

        Keyframe outKeyframe = keyframes[index - 1];
        Keyframe inKeyframe = keyframes[index];

        var dX = inKeyframe.time - outKeyframe.time;
        var dY = inKeyframe.value - outKeyframe.value;

        outKeyframe.weightedMode = WeightedMode.Both;
        inKeyframe.weightedMode = WeightedMode.Both;
        if (Mathf.Approximately(dY, 0f) || Mathf.Approximately(dX, 1 / 30f))    //没有变化的就不需要补间插值了
        {
            outKeyframe.outTangent = 0;
            outKeyframe.outWeight = 0;

            inKeyframe.inTangent = 0;
            inKeyframe.inWeight = 0;
        }
        else
        {
            //插值计算[0~127]
            //参考https://www.jianshu.com/p/ae312fb53fc3
            Vector2 p0 = new Vector2(outKeyframe.time, outKeyframe.value);
            Vector2 p3 = new Vector2(inKeyframe.time, inKeyframe.value);
            Vector2 p1 = Vector2.zero;
            Vector2 p2 = Vector2.zero;
            var intTuple = GetInterpolationPoints(curCameraData.interpolation, type);
            var ptTuple = ConvertToFramekeyControllerPoint(intTuple.Item1, intTuple.Item2, outKeyframe, inKeyframe);    //转化为keyFrame的控制点
            p1 = ptTuple.Item1;
            p2 = ptTuple.Item2;

            float[] coeffs = CalculateBezierCoefficient(p0, p1, p2, p3);
            outKeyframe.outTangent = coeffs[0];
            outKeyframe.outWeight = coeffs[1];
            inKeyframe.inTangent = coeffs[2];
            inKeyframe.inWeight = coeffs[3];
        }

        //因为是结构体,所以需要重新赋值
        keyframes[index - 1] = outKeyframe;
        keyframes[index] = inKeyframe;
    }

    private static Keyframe[] OptimizedCurves(int type, Keyframe[] keyframes, VMDFormat.CameraList cameraList)
    {
        List<Keyframe> framesList = new List<Keyframe>();

        //曲线优化
        for (int i = 0; i < keyframes.Length; i++)
        {
            var keyframe = keyframes[i];
            framesList.Add(keyframe);
        }

        //针对只有一帧的进行优化
        if (framesList.Count == 1)
        {
            Keyframe[] newKeyframes = new Keyframe[2];
            newKeyframes[0] = keyframes[0];
            newKeyframes[1] = keyframes[0];
            newKeyframes[1].time += 0.001f / 60f;//1[ms]
            newKeyframes[0].outTangent = 0f;
            newKeyframes[1].inTangent = 0f;

            framesList.Clear();
            framesList.AddRange(newKeyframes);
        }

        return framesList.ToArray();
    }
    private static Tuple<Vector2, Vector2> GetInterpolationPoints(byte[] interpolation, int type)
    {
        int row = (type - 1) * 4;
        Vector2 p1 = new Vector2(interpolation[row + 0], interpolation[row + 2]);
        Vector2 p2 = new Vector2(interpolation[row + 1], interpolation[row + 3]);

        return new Tuple<Vector2, Vector2>(p1, p2);
    }

    //把MMD补间曲线中p1,p2的点映射回Curve中去
    private static Tuple<Vector2, Vector2> ConvertToFramekeyControllerPoint(Vector2 p1, Vector2 p2, Keyframe outKeyframe, Keyframe inKeyframe)
    {
        var dX = inKeyframe.time - outKeyframe.time;
        var dY = inKeyframe.value - outKeyframe.value;

        var newP1 = new Vector2(outKeyframe.time + p1.x / 127 * dX, outKeyframe.value + p1.y / 127 * dY);
        var newP2 = new Vector2(outKeyframe.time + p2.x / 127 * dX, outKeyframe.value + p2.y / 127 * dY);

        //因为不存在90度的情况,这里要趋近,但是也不能太趋近,不然补间前几帧会变的陡峭
        if (Mathf.Approximately(outKeyframe.time, newP1.x)) newP1.x += 0.1f;
        if (Mathf.Approximately(inKeyframe.time, newP2.x)) newP2.x -= 0.1f;

        return new Tuple<Vector2, Vector2>(newP1, newP2);
    }

    private static float Tangent(in Vector2 from, in Vector2 to)
    {
        Vector2 vec = to - from;
        return vec.y / vec.x;
    }

    private static float Weight(in Vector2 from, in Vector2 to, float length)
    {
        return (to.x - from.x) / length;
    }

    //根据四个控制点计算三次贝塞尔曲线的系数
    //插值:贝塞尔曲线插值(四个点)
    //https://blog.csdn.net/seizeF/article/details/96368503
    //return {outTangent,outWeight,inTangent,inWeight)
    private static float[] CalculateBezierCoefficient(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float p30Length = (p3.x - p0.x);

        float outTangent = Tangent(p0, p1);
        float outWeight = Weight(p0, p1, p30Length);

        float inTangent = Tangent(p2, p3);
        float inWeight = Weight(p2, p3, p30Length);

        return new float[] { outTangent, outWeight, inTangent, inWeight };
    }
}