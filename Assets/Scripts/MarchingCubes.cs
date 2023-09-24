using System.Collections;
using UnityEngine;

public class MarchingCubes : MonoBehaviour
{
    static MarchingCubes instance;
    public static MarchingCubes Instance { get => instance; }

    [SerializeField] float isoValue = 0f;
    [SerializeField] ComputeShader marchingCubesShader;
    ComputeBuffer bufferTriangles;
    ComputeBuffer bufferTrianglesCount;
    ComputeBuffer bufferDensityValues;

    int nbPointsPerChunk;
    int ThreadGroups;

    struct Triangle
    {
        Vector3 A;
        Vector3 B;
        Vector3 C;

        public Vector3 this[int i]
        {
            get => i == 0 ? A : i == 1 ? B : C;
        }
    };

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void InitBuffers(int _currentLOD)
    {
        nbPointsPerChunk = MapParameters.GetPointsPerChunk(_currentLOD);
        ThreadGroups = MapParameters.ThreadGroups(_currentLOD);

        //Count : 5 triangles max per Cube * All cubes per Chunk
        //Size : triangles = 3 vertices, so 3 floats per vertex 
        int allPointsPerChunk = nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk;
        bufferTriangles = new ComputeBuffer(5 * allPointsPerChunk, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        //ComputeBufferType.Raw because it's just a count of triangles
        bufferTrianglesCount = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        //DensityValues computed from Noise
        bufferDensityValues = new ComputeBuffer(allPointsPerChunk, sizeof(float));
    }

    public void Edit(float[] _density, Vector3 _hitPos, Vector3 _chunkPos, float _radiusTerraforming, bool _isConstruct, float _strengh, int _currentLOD)
    {
        marchingCubesShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
        marchingCubesShader.SetInt("_ChunkAxisSize", MapParameters.GetChunkAxisSize());
        bufferDensityValues.SetData(_density);

        int terraformingIndex = marchingCubesShader.FindKernel("Terraforming");

        marchingCubesShader.SetBuffer(terraformingIndex, "_DensityValues", bufferDensityValues);
        marchingCubesShader.SetVector("_HitPos", _hitPos);
        marchingCubesShader.SetVector("_ChunkPos", _chunkPos);
        marchingCubesShader.SetFloat("_RadiusTerraforming", _radiusTerraforming);
        marchingCubesShader.SetFloat("_TerraformStrength", _isConstruct ? _strengh : -_strengh);

        marchingCubesShader.Dispatch(terraformingIndex, ThreadGroups, ThreadGroups, ThreadGroups);

        bufferDensityValues.GetData(_density);
    }

    public Mesh Compute(in float[] _density, int _currentLOD, bool _isEdit = false)
    {    
        if (!_isEdit)
        {
            InitBuffers(_currentLOD);
            marchingCubesShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
            marchingCubesShader.SetInt("_ChunkAxisSize", MapParameters.GetChunkAxisSize());
            bufferDensityValues.SetData(_density);
        }
        int marchingCubesIndex = marchingCubesShader.FindKernel("GenerateMarchingCubes");

        marchingCubesShader.SetBuffer(marchingCubesIndex, "_DensityValues", bufferDensityValues);
        marchingCubesShader.SetBuffer(marchingCubesIndex, "_Triangles", bufferTriangles);
        bufferTriangles.SetCounterValue(0);

        marchingCubesShader.SetFloat("_IsoValue", isoValue);

        marchingCubesShader.Dispatch(marchingCubesIndex, ThreadGroups, ThreadGroups, ThreadGroups);

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

        Mesh chunkMesh = new Mesh();
        chunkMesh.Clear();
        chunkMesh.vertices = vertices;
        chunkMesh.triangles = triangles;
        chunkMesh.RecalculateNormals();

        ReleaseBuffers();
        return chunkMesh;
    }

    void ReleaseBuffers()
    {
        bufferTriangles.Release();
        bufferTrianglesCount.Release();
        bufferDensityValues.Release();
    }
}
