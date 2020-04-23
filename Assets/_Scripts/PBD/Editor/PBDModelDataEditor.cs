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
        }
    }
}