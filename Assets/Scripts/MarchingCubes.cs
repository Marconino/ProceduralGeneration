using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MarchingCubes : MonoBehaviour
{
    static MarchingCubes instance;
    public static MarchingCubes Instance { get => instance; }

    [SerializeField] float isoValue = 0f;
    [SerializeField] ComputeShader marchingCubesShader;
    ComputeBuffer bufferTriangles;
    ComputeBuffer bufferTrianglesCount;
    int nbPointsPerChunk;

    struct Triangle
    {
        Vector3 A;
        Vector3 B;
        Vector3 C;

        public Vector3 this [int i]
        {
            get => i == 0 ? A : i == 1 ? B : C;
        }
    };

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void CreateBuffers(int _nbPointsPerChunk)
    {
        nbPointsPerChunk = _nbPointsPerChunk;

        //Count : 5 triangles max per Cube * All cubes per Chunk
        //Size : triangles = 3 vertices, so 3 floats per vertex 
        int allPointsPerChunk = _nbPointsPerChunk * _nbPointsPerChunk * _nbPointsPerChunk;
        bufferTriangles = new ComputeBuffer(5 * allPointsPerChunk, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        //ComputeBufferType.Raw because it's just a count of triangles
        bufferTrianglesCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);


        Noise.Instance.CreateBuffers(_nbPointsPerChunk);
    }

    public void Compute(ref Mesh _chunkMesh, int _x, int _y, int _z)
    {
        marchingCubesShader.SetBuffer(0, "_DensityValues", Noise.Instance.Compute(_x, _y, _z));

        marchingCubesShader.SetBuffer(0, "_Triangles", bufferTriangles);
        marchingCubesShader.SetInt("_BlocksPerChunk", nbPointsPerChunk);
        marchingCubesShader.SetFloat("_IsoValue", isoValue);

        bufferTriangles.SetCounterValue(0);

        marchingCubesShader.Dispatch(0, nbPointsPerChunk / 8, nbPointsPerChunk / 8, nbPointsPerChunk / 8);

        ComputeBuffer.CopyCount(bufferTriangles, bufferTrianglesCount, 0);
        int[] trianglesCount = { 0 };
        bufferTrianglesCount.GetData(trianglesCount);
        Triangle[] trianglesValues = new Triangle[trianglesCount[0]];
        bufferTriangles.GetData(trianglesValues);

        Vector3[] vertices = new Vector3[trianglesCount[0] * 3];
        int[] triangles = new int[trianglesCount[0] * 3];

        for (int i = 0; i < trianglesCount[0]; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                vertices[i * 3 + j] = trianglesValues[i][j];
                triangles[i * 3 + j] = i * 3 + j;
            }
        }

        _chunkMesh.vertices = vertices;
        _chunkMesh.triangles = triangles;
        _chunkMesh.RecalculateNormals();
    }

    public void ReleaseBuffers()
    {
        bufferTriangles.Release();
        bufferTrianglesCount.Release();
        Noise.Instance.ReleaseBuffers();
    }
}
