using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

public class MarchingCubesGenerator : MonoBehaviour
{
    public class MarchingCubes : BaseComputeShader<Mesh>
    {
        public struct Triangle
        {
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;
        };

        DataCompute<Triangle> triangles;
        DataCompute<int> trianglesCount;
        DataCompute<float> density;

        public MarchingCubes(int _lod, in NativeArray<float> _densityValues, int _x, int _y, int _z) : base(_lod, _x, _y, _z)
        {
            density.data = _densityValues;
        }

        protected override void CreateDataCompute()
        {
            triangles = new DataCompute<Triangle>();
            trianglesCount = new DataCompute<int>();
            density = new DataCompute<float>();
        }

        protected override void InitBuffers()
        {
            //Count : 5 triangles max per Cube * All cubes per Chunk
            //Size : triangles = 3 vertices, so 3 floats per vertex 
            int allPointsPerChunk = nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk;
            triangles.computeBuffer = new ComputeBuffer(5 * allPointsPerChunk, sizeof(float) * 3 * 3, ComputeBufferType.Append);

            //ComputeBufferType.Raw because it's just a count of triangles
            trianglesCount.computeBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

            //DensityValues computed from Noise
            density.computeBuffer = new ComputeBuffer(allPointsPerChunk, sizeof(float));
        }

        protected override void ReleaseBuffers()
        {
            triangles.computeBuffer.Release();
            trianglesCount.computeBuffer.Release();
            density.computeBuffer.Release();
        }

        protected override void StartRequest()
        {
            ComputeShader marchingCubesShader = instance.marchingCubesShader;
            int kernel = marchingCubesShader.FindKernel("GenerateMarchingCubes");

            marchingCubesShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
            marchingCubesShader.SetInt("_ThreadGroups", threadGroups);
            marchingCubesShader.SetInt("_ChunkAxisSize", MapParameters.GetChunkAxisSize());
            density.computeBuffer.SetData(density.data);


            marchingCubesShader.SetBuffer(kernel, "_DensityValues", density.computeBuffer);
            marchingCubesShader.SetBuffer(kernel, "_Triangles", triangles.computeBuffer);
            triangles.computeBuffer.SetCounterValue(0);

            marchingCubesShader.SetFloat("_IsoValue", instance.isoValue);

            marchingCubesShader.Dispatch(kernel, threadGroups, threadGroups, threadGroups);

            ComputeBuffer.CopyCount(triangles.computeBuffer, trianglesCount.computeBuffer, 0);

            AsyncGPUReadback.Request(trianglesCount.computeBuffer, trianglesCount.OnReadbackData);
            AsyncGPUReadback.Request(triangles.computeBuffer, triangles.OnReadbackData);
        }

        unsafe public override Mesh GetComputeAsync()
        {
            int nbTriangles = trianglesCount.data[0];
            NativeArray<Triangle> trianglesValues = triangles.data;
            
            Vector3[] vertices = new Vector3[nbTriangles * 3];
            int[] trianglesIndex = new int[nbTriangles * 3];
            void* ptr = trianglesValues.GetUnsafePtr();

            for (int i = 0; i < nbTriangles; i++)
            {
                Triangle currTriangle = UnsafeUtility.ArrayElementAsRef<Triangle>(ptr, i);

                for (int j = 0; j < 3; j++)
                {
                    vertices[i * 3 + j] = j == 0 ? currTriangle.A : j == 1 ? currTriangle.B : currTriangle.C;
                    trianglesIndex[i * 3 + j] = i * 3 + j;
                }
            }

            Mesh chunkMesh = new Mesh();
            chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            chunkMesh.Clear();
            chunkMesh.vertices = vertices;
            chunkMesh.triangles = trianglesIndex;
            chunkMesh.RecalculateNormals();

            return chunkMesh;
        }

        public override bool IsComputed()
        {
            return triangles.data.IsCreated && trianglesCount.data.IsCreated;
        }

        public override void DisposeData()
        {
            trianglesCount.data.Dispose();
            triangles.data.Dispose();
        }
    }

    static MarchingCubesGenerator instance;
    public static MarchingCubesGenerator Instance { get => instance; }

    [Header("MC Parameters")]
    [SerializeField] float isoValue = 0f;
    [SerializeField] ComputeShader marchingCubesShader;
    Queue<MarchingCubes> marchingCubesQueue;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            marchingCubesQueue = new Queue<MarchingCubes>();
        }
    }

    public MarchingCubes CreateMCInstance(int _lod, in NativeArray<float> _densityValues, int _x, int _y, int _z)
    {
        MarchingCubes marchingCubes = new MarchingCubes(_lod, _densityValues, _x, _y, _z);
        marchingCubesQueue.Enqueue(marchingCubes);
        return marchingCubes;
    }

    public MarchingCubes DequeueMCComputed()
    {
        return marchingCubesQueue.Dequeue();
    }

    public bool HasAComputedMC()
    {
        return marchingCubesQueue.Count > 0 && marchingCubesQueue.Peek().IsComputed();
    }
}
