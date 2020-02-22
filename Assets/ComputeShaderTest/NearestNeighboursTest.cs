using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

public class NearestNeighboursTest : MonoBehaviour
{
    
    public ComputeShader _shader;

    private static int ARRAY_LEN = 1024;
    private static ComputeBuffer _pos_buf;
//    private static ComputeBuffer _dist_buf;

    void Start()
    {
        _pos_buf = new ComputeBuffer(ARRAY_LEN, 3 * sizeof(float));
//        _dist_buf = new ComputeBuffer(ARRAY_LEN * ARRAY_LEN, sizeof(float));

        Shader.SetGlobalBuffer(Shader.PropertyToID("positions"), _pos_buf);
//        Shader.SetGlobalBuffer(Shader.PropertyToID("distances"), _dist_buf);

        float[] pos_data = new float[ARRAY_LEN * 3];
//        float[] _dist_data = new float[ARRAY_LEN * ARRAY_LEN];

        for (int i = 0; i < ARRAY_LEN; i++)
        {
            float rand_x = (0.5f - (i % 256) / 256.0f) * 16.0f;
            float rand_y = (0.5f - (i / 256) / 256.0f) * 16.0f;

            pos_data[i * 3 + 0] = rand_x;
            pos_data[i * 3 + 1] = rand_y;
            pos_data[i * 3 + 2] = 0.0f;
        }

        _pos_buf.SetData(pos_data);
        var a = new NativeArray<float>();
//        _dist_buf.SetData(_dist_data);

//        _dist_buf.GetData(_dist_data);

//        int x = 20;
//        int y = 38;
//        float dist = Vector3.Distance(new Vector3(pos_data[x * 3], pos_data[x * 3 + 1], pos_data[x * 3 + 2]),
//            new Vector3(pos_data[y * 3], pos_data[y * 3 + 1], pos_data[y * 3 + 2]));

//        float shader_dist = _dist_data[x * ARRAY_LEN + y];
        
//        Debug.Log($"{shader_dist} - {dist}");
    }

    private void Update()
    {
        long ticks = DateTime.Now.Ticks;
        _shader.Dispatch(_shader.FindKernel("CSMain"), 1, 1, 1);
        print(ticks - DateTime.Now.Ticks);

    }

    void OnDestroy()
    {
        _pos_buf.Dispose();
//        _dist_buf.Dispose();
    }
}