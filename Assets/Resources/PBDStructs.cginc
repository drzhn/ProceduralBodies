#ifndef __PBDSTRUCTS_INCLUDED__
#define __PBDSTRUCTS_INCLUDED__

struct PBDPointInfo
{
     bool valid;
     float mass;
     float radius;
};

struct PBDConnectionInfo
{
    int pointIndex;
    float stiffness;
    float distance;
};

struct PBDBoneInfo
{
    float3 position;
    int parentIndex;
};

struct PBDUnitData // 28 bytes
{
    bool valid;
    int hipsIndex;
    int neckIndex;
    
    int hipsNeighbour1;
    int hipsNeighbour2;
    int neckNeighbour1;
    int neckNeighbour2;
};

struct PBDSkeletonData // 112 bytes
{
    int3 leftHand;
    int leftHandInverseX;
    float3 leftHandUp;
    
    int3 rightHand;
    int rightHandInverseX;
    float3 rightHandUp;
    
    int3 leftFoot;
    int leftFootInverseX;
    float3 leftFootUp;
    
    int3 rightFoot;
    int rightFootInverseX;
    float3 rightFootUp;
};

struct PBDSkeletonBoneTransform // 68 bytes
{
    float4x4 Trs;
    int ParentIndex;
};

#endif // __PBDSTRUCTS_INCLUDED__