using System;
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

        //Generator
        MarchingCubesGenerator generator;

        public MarchingCubes(MarchingCubesGenerator _generator, in NativeArray<float> _densityValues, int _lod, int _x, int _y, int _z) : base(_lod, _x, _y, _z)
        {
            generator = _generator;
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
            triangles.computeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, 5 * allPointsPerChunk, sizeof(float) * 3 * 3);

            //ComputeBufferType.Raw because it's just a count of triangles
            trianglesCount.computeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 1, sizeof(int));

            //DensityValues computed from Noise
            density.computeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, allPointsPerChunk, sizeof(float));
        }

        protected override void ReleaseBuffers()
        {
            triangles.computeBuffer.Release();
            trianglesCount.computeBuffer.Release();
            density.computeBuffer.Release();
        }

        protected override void StartRequest()
        {
            ComputeShader marchingCubesShader = generator.marchingCubesShader;
            int kernel = marchingCubesShader.FindKernel("GenerateMarchingCubes");

            marchingCubesShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
            marchingCubesShader.SetInt("_ThreadGroups", threadGroups);
            marchingCubesShader.SetInt("_ChunkAxisSize", MapParameters.GetChunkAxisSize());
            density.computeBuffer.SetData(density.data);

            marchingCubesShader.SetBuffer(kernel, "_DensityValues", density.computeBuffer);
            marchingCubesShader.SetBuffer(kernel, "_Triangles", triangles.computeBuffer);

            triangles.computeBuffer.SetCounterValue(0);

            marchingCubesShader.SetFloat("_IsoValue", generator.isoValue);

            marchingCubesShader.Dispatch(kernel, threadGroups, threadGroups, threadGroups);

            GraphicsBuffer.CopyCount(triangles.computeBuffer, trianglesCount.computeBuffer, 0);

            AsyncGPUReadback.Request(trianglesCount.computeBuffer, trianglesCount.OnReadbackData);
            AsyncGPUReadback.Request(triangles.computeBuffer, triangles.OnReadbackData);
        }

        unsafe public override Mesh GetComputeAsync()
        {
            int nbTriangles = trianglesCount.data[0];
            Vector3[] vertices = new Vector3[nbTriangles * 3];
            int[] trianglesIndex = new int[nbTriangles * 3];
            Color[] vertexColors = new Color[nbTriangles * 3];
            void* ptr = triangles.data.GetUnsafePtr();

            float minHeight = -8000;
            float maxHeight = 8000;
            Color colorLow = Color.blue;
            Color colorMid = new Color(0.6f, 0.3f, 0.1f, 1f);
            Color colorHigh = Color.white;

            for (int i = 0; i < nbTriangles; i++)
            {
                Triangle currTriangle = UnsafeUtility.ArrayElementAsRef<Triangle>(ptr, i);

                for (int j = 0; j < 3; j++)
                {
                    vertices[i * 3 + j] = j == 0 ? currTriangle.A : j == 1 ? currTriangle.B : currTriangle.C;
                    trianglesIndex[i * 3 + j] = i * 3 + j;

                    float currentVertexWorld = vertices[i * 3 + j].y + pos.world.y;
                    float normalizedHeight = (currentVertexWorld - minHeight) / (maxHeight - minHeight);
                    // Interpoler les couleurs en fonction de la hauteur normalisée
                    Color vertexColor;
                    if (normalizedHeight < 0.3f)
                    {
                        // Interpolation entre bleu et marron pour les altitudes basses à moyennes
                        vertexColor = colorLow;
                    }
                    else if (normalizedHeight < 0.7f)
                    {
                        // Interpolation entre marron et blanc pour les altitudes moyennes à élevées
                        vertexColor = colorMid;
                    }
                    else
                    {
                        vertexColor = colorHigh;
                    }

                    vertexColors[i * 3 + j] = vertexColor;
                }
            }

            Mesh chunkMesh = new Mesh();
            chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            chunkMesh.Clear();
            chunkMesh.SetVertices(vertices);
            chunkMesh.SetTriangles(trianglesIndex, 0);
            chunkMesh.SetColors(vertexColors);
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

    [Header("Computing Parameters")]
    [SerializeField] ComputeShader marchingCubesShader;
    [SerializeField] int concurrentComputing = 5;

    [Header("MC Parameters")]
    [SerializeField] float isoValue = 0f;
    List<MarchingCubes> marchingCubesQueue;

    MarchingCubesGenerator()
    {
        marchingCubesQueue = new List<MarchingCubes>();
    }

    public MarchingCubes CreateMCInstance( in NativeArray<float> _densityValues, int _lod, int _x, int _y, int _z)
    {
        MarchingCubes marchingCubes = new MarchingCubes(this, _densityValues, _lod, _x, _y, _z);
        marchingCubesQueue.Add(marchingCubes);
        return marchingCubes;
    }

    public MarchingCubes DequeueMCComputed()
    {
        MarchingCubes marchingCubes = marchingCubesQueue[0];
        marchingCubesQueue.Remove(marchingCubes);
        return marchingCubes;
    }
    public MarchingCubes[] DequeueMCsComputed()
    {
        int limit = marchingCubesQueue.Count > concurrentComputing ? concurrentComputing : marchingCubesQueue.Count;

        MarchingCubes[] mcsReturn = new MarchingCubes[limit ];
        marchingCubesQueue.CopyTo(0, mcsReturn, 0, limit);
        marchingCubesQueue.RemoveRange(0, limit);
        return mcsReturn;
    }
    public bool HasAComputedMC()
    {
        return marchingCubesQueue.Count > 0 && marchingCubesQueue[0].IsComputed();
    }

    public void ClearOldMC()
    {
        for (int i = 0; i < marchingCubesQueue.Count; i++)
        {
            marchingCubesQueue[i].DisposeData();
        }
        marchingCubesQueue.Clear();
    }
}
