using System.Collections;
using UnityEngine;

public class Noise : MonoBehaviour
{
    static Noise instance;
    public static Noise Instance { get => instance; }

    [Header("Noise Parameters")]
    [SerializeField] int seed;
    [SerializeField, Min(1)] int octaves = 1;
    [SerializeField] float amplitude = 5f;
    [SerializeField] float frequency = 0.005f;
    [SerializeField] float lacunarity = 2f;
    [SerializeField] float groundOffset = 0.2f;

    [SerializeField] ComputeShader noiseShader;
    ComputeBuffer bufferDensityValues;
    float[] densityValues;

    int nbPointsPerChunk;
    int ThreadGroups;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void InitBuffers(int _currentLOD)
    {
        nbPointsPerChunk = MapParameters.GetPointsPerChunk(_currentLOD);
        ThreadGroups = MapParameters.ThreadGroups(_currentLOD);

        int allPointsPerChunk = nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk;
        
        bufferDensityValues = new ComputeBuffer(allPointsPerChunk, sizeof(float));
        densityValues = new float[allPointsPerChunk];
    }

    public float[] Compute(float _x, float _y, float _z, int _currentLOD)
    {
        InitBuffers(_currentLOD);
        noiseShader.SetBuffer(0, "_DensityValues", bufferDensityValues);

        noiseShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
        noiseShader.SetInt("_Octaves", octaves);
        noiseShader.SetInt("_Seed", seed);
        noiseShader.SetFloat("_Amplitude", amplitude);
        noiseShader.SetFloat("_Frequency", frequency);
        noiseShader.SetFloat("_Lacunarity", lacunarity);
        noiseShader.SetFloat("_GroundOffset", groundOffset);
        noiseShader.SetFloat("_OffsetX", _x);
        noiseShader.SetFloat("_OffsetY", _y);
        noiseShader.SetFloat("_OffsetZ", _z);

        noiseShader.Dispatch(0, ThreadGroups, ThreadGroups, ThreadGroups);
        bufferDensityValues.GetData(densityValues);
        ReleaseBuffers();
        return densityValues;
    }

    void ReleaseBuffers()
    {
        bufferDensityValues.Release();
    }
}
