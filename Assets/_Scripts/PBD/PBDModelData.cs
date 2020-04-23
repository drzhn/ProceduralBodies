using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PBD
{
    [CreateAssetMenu(fileName = "PBDModelData", menuName = "PBDModelData", order = 5)]
    public class PBDModelData : ScriptableObject
    {
        public GameObject Prefab;
        public Mesh Mesh;
        public Matrix4x4[] BindPoses;
        public PBDSkeletonBoneTransform[] SkeletonBoneTransforms;
        public int RootBoneIndex;
    }
}