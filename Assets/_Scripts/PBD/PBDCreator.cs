using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBD
{
    public class PBDCreator : MonoBehaviour
    {
        [SerializeField] private int amount;
        [SerializeField] private GameObject prefab;

        private void Awake()
        {
            GameObject[,,] points = new GameObject[amount, amount, amount];

            for (int x = 0; x < amount; x++)
            {
                for (int y = 0; y < amount; y++)
                {
                    for (int z = 0; z < amount; z++)
                    {
                        points[x, y, z] = prefab != null ? Instantiate(prefab) : new GameObject("Point");
                        points[x, y, z].transform.SetParent(transform);
                        points[x, y, z].transform.localPosition = Vector3.up + new Vector3(x, y, z);
                    }
                }
            }

            int edgesAmount = 0;

            for (int x = 0; x < amount; x++)
            {
                for (int y = 0; y < amount; y++)
                {
                    for (int z = 0; z < amount; z++)
                    {
                        if (x + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x + 1, y, z].transform;
                            edgesAmount++;
                        }

                        if (y + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x, y + 1, z].transform;
                            edgesAmount++;
                        }

                        if (z + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x, y, z + 1].transform;
                            edgesAmount++;
                        }

                        if (x + 1 < amount && y + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x + 1, y + 1, z].transform;
                            edgesAmount++;
                        }

                        if (z + 1 < amount && y + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x, y + 1, z + 1].transform;
                            edgesAmount++;
                        }

                        if (x - 1 >= 0 && z + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x - 1, y, z + 1].transform;
                            edgesAmount++;
                        }

                        if (x - 1 >= 0 && y - 1 >= 0 && z + 1 < amount)
                        {
                            var constraint = points[x, y, z].AddComponent<PBDConnectionTest>();
                            constraint.connectedPoint = points[x - 1, y - 1, z + 1].transform;
                            edgesAmount++;
                        }
                    }
                }
            }
            Debug.Log(amount*amount*amount);
            Debug.Log(edgesAmount);
        }
    }
}