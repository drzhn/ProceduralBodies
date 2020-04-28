using System;
using UnityEditor;
using UnityEngine;

namespace PBD
{
    [CustomEditor(typeof(PBDModelData))]
    [CanEditMultipleObjects]
    public class PBDModelDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Prefab"));
            if (GUILayout.Button("Update bone data"))
            {
                UpdateBoneData();
            }

            EditorGUILayout.LabelField("[ATTENTION] Try not to change these properties!");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkeletonData"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Mesh"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RootBoneIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("BindPoses"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SkeletonBoneTransforms"));
            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateBoneData()
        {
            PBDModelData obj = (PBDModelData) target;
            if (obj.Prefab == null)
            {
                Debug.LogError("Prefab is null");
                return;
            }

            SkinnedMeshRenderer rend = obj.Prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = rend.sharedMesh;
            obj.Mesh = mesh;
            obj.BindPoses = new Matrix4x4[mesh.bindposes.Length];
            Array.Copy(mesh.bindposes, obj.BindPoses, mesh.bindposes.Length);

            var bones = rend.bones;
            var boneCount = bones.Length;
            obj.SkeletonBoneTransforms = new PBDSkeletonBoneTransform[boneCount];

            for (var i = 0; i < boneCount; i++)
            {
                obj.SkeletonBoneTransforms[i] = new PBDSkeletonBoneTransform()
                {
                    Trs = Utils.GetTRS(bones[i].localPosition, bones[i].localRotation, bones[i].localScale),
                    ParentIndex = bones[i] == rend.rootBone ? -1 : Array.IndexOf(bones, bones[i].parent)
                };

                if (obj.SkeletonBoneTransforms[i].ParentIndex == -1) obj.RootBoneIndex = i;
            }

            var skeletonData = new PBDSkeletonData();
            PBDSkeletonBoneContainer boneContainer = obj.Prefab.GetComponentInChildren<PBDSkeletonBoneContainer>();
            for (var i = 0; i < 3; i++)
            {
                skeletonData.leftHand[i] = Array.IndexOf(bones, boneContainer.LeftHand[i]);
                skeletonData.rightHand[i] = Array.IndexOf(bones, boneContainer.RightHand[i]);
                skeletonData.leftFoot[i] = Array.IndexOf(bones, boneContainer.LeftFoot[i]);
                skeletonData.rightFoot[i] = Array.IndexOf(bones, boneContainer.RightFoot[i]);
            }

            skeletonData.leftHandInverseX = Vector3.Dot(boneContainer.LeftHand[0].right.normalized, boneContainer.LeftHand[1].position - boneContainer.LeftHand[0].position) < 0 ? 1 : -1;
            skeletonData.rightHandInverseX = Vector3.Dot(boneContainer.RightHand[0].right.normalized, boneContainer.RightHand[1].position - boneContainer.RightHand[0].position) < 0 ? 1 : -1;
            skeletonData.leftFootInverseX = Vector3.Dot(boneContainer.LeftFoot[0].right.normalized, boneContainer.LeftFoot[1].position - boneContainer.LeftFoot[0].position) < 0 ? 1 : -1;
            skeletonData.rightFootInverseX = Vector3.Dot(boneContainer.RightFoot[0].right.normalized, boneContainer.RightFoot[1].position - boneContainer.RightFoot[0].position) < 0 ? 1 : -1;

            skeletonData.leftHandUp = boneContainer.LeftHand[0].up;
            skeletonData.rightHandUp = boneContainer.RightHand[0].up;
            skeletonData.leftFootUp = boneContainer.LeftFoot[0].up;
            skeletonData.rightFootUp = boneContainer.RightFoot[0].up;
            
            
            obj.SkeletonData = skeletonData;

            int lefthand = 32;
            Vector4 position = bones[lefthand].position;
            LogVector4(position);
            LogVector4(bones[lefthand].localPosition);

            position = bones[lefthand].localToWorldMatrix * (new Vector4(0, 0, 0, 1));
            LogVector4(position);
            LogVector4(bones[lefthand].parent.worldToLocalMatrix * position);

            Debug.Log(bones[lefthand].localToWorldMatrix);
            Debug.Log(inverse(bones[lefthand].worldToLocalMatrix));
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // l2w * v_l = v_w 
        }

        private void LogVector4(Vector4 v)
        {
            Debug.Log($"{v.x} {v.y} {v.z} {v.w}");
        }

        private Matrix4x4 inverse(Matrix4x4 m)
        {
            float n11 = m.m00, n12 = m.m10, n13 = m.m20, n14 = m.m30;
            float n21 = m.m01, n22 = m.m11, n23 = m.m21, n24 = m.m31;
            float n31 = m.m02, n32 = m.m12, n33 = m.m22, n34 = m.m32;
            float n41 = m.m03, n42 = m.m13, n43 = m.m23, n44 = m.m33;

            float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
            float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
            float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
            float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

            float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
            Debug.Log($"DET: {det}");
            float idet = 1.0f / det;

            Matrix4x4 ret;

            ret.m00 = t11 * idet;
            ret.m01 = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
            ret.m02 = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
            ret.m03 = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;
            ret.m10 = t12 * idet;
            ret.m11 = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
            ret.m12 = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
            ret.m13 = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;
            ret.m20 = t13 * idet;
            ret.m21 = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
            ret.m22 = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
            ret.m23 = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;
            ret.m30 = t14 * idet;
            ret.m31 = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
            ret.m32 = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
            ret.m33 = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

            return ret;
        }
    }
}