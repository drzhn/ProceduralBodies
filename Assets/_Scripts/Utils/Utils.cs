using UnityEngine;

public static class Utils
{
    public static Matrix4x4 GetTRS(Vector3 pos, Quaternion q, Vector3 s)
    {
        Matrix4x4 result = new Matrix4x4();
        // Rotation and Scale
        // Quaternion multiplication can be used to represent rotation. 
        // If a quaternion is represented by qw + i qx + j qy + k qz , then the equivalent matrix for rotation is (including scale):
        // Remarks: https://forums.inovaestudios.com/t/math-combining-a-translation-rotation-and-scale-matrix-question-to-you-math-magicians/5194/2
        float sqw = q.w * q.w;
        float sqx = q.x * q.x;
        float sqy = q.y * q.y;
        float sqz = q.z * q.z;
        result.m00 = (1 - 2 * sqy - 2 * sqz) * s.x;
        result.m01 = (2 * q.x * q.y - 2 * q.z * q.w);
        result.m02 = (2 * q.x * q.z + 2 * q.y * q.w);
        result.m10 = (2 * q.x * q.y + 2 * q.z * q.w);
        result.m11 = (1 - 2 * sqx - 2 * sqz) * s.y;
        result.m12 = (2 * q.y * q.z - 2 * q.x * q.w);
        result.m20 = (2 * q.x * q.z - 2 * q.y * q.w);
        result.m21 = (2 * q.y * q.z + 2 * q.x * q.w);
        result.m22 = (1 - 2 * sqx - 2 * sqy) * s.z;
        // Translation
        result.m03 = pos.x;
        result.m13 = pos.y;
        result.m23 = pos.z;
        result.m33 = 1.0f;
        // Return result
        return result;
    }

    public static Matrix4x4 Rotate(Quaternion q)
    {
        Matrix4x4 result = new Matrix4x4();
        float sqw = q.w * q.w;
        float sqx = q.x * q.x;
        float sqy = q.y * q.y;
        float sqz = q.z * q.z;
        result.m00 = (1 - 2 * sqy - 2 * sqz);
        result.m01 = (2 * q.x * q.y - 2 * q.z * q.w);
        result.m02 = (2 * q.x * q.z + 2 * q.y * q.w);
        result.m10 = (2 * q.x * q.y + 2 * q.z * q.w);
        result.m11 = (1 - 2 * sqx - 2 * sqz);
        result.m12 = (2 * q.y * q.z - 2 * q.x * q.w);
        result.m20 = (2 * q.x * q.z - 2 * q.y * q.w);
        result.m21 = (2 * q.y * q.z + 2 * q.x * q.w);
        result.m22 = (1 - 2 * sqx - 2 * sqy);
        result.m33 = 1.0f;
        return result;
    }
}