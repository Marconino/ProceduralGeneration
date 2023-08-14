using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    [SerializeField] float offsetX;
    [SerializeField] float offsetY;
    [SerializeField] float offsetZ;
    int nbPointsPerChunk;

    [SerializeField] ComputeShader noiseShader;
    ComputeBuffer bufferDensityValues;
    System.Random random;

    void Awake()
    {
        if (instance == null)
            instance = this;

        random = new System.Random();
    }

    public void CreateBuffers(int _nbPointsPerChunk)
    {
        nbPointsPerChunk = _nbPointsPerChunk;
        bufferDensityValues = new ComputeBuffer(nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk, sizeof(float));
    }

    public float[] Compute(int _x, int _y, int _z)
    {
        noiseShader.SetBuffer(0, "_DensityValues", bufferDensityValues);

        offsetX = _x * nbPointsPerChunk;
        offsetY = _y * nbPointsPerChunk;
        offsetZ = _z * nbPointsPerChunk;

        noiseShader.SetInt("_BlocksPerChunk", nbPointsPerChunk);
        noiseShader.SetInt("_Octaves", octaves);
        noiseShader.SetInt("_Seed", seed);
        noiseShader.SetFloat("_Amplitude", amplitude);
        noiseShader.SetFloat("_Frequency", frequency);
        noiseShader.SetFloat("_Lacunarity", lacunarity);
        noiseShader.SetFloat("_GroundOffset", groundOffset);
        noiseShader.SetFloat("_OffsetX", offsetX);
        noiseShader.SetFloat("_OffsetY", offsetY);
        noiseShader.SetFloat("_OffsetZ", offsetZ);

        noiseShader.Dispatch(0, nbPointsPerChunk / 8, nbPointsPerChunk / 8, nbPointsPerChunk / 8);

        float[] densityValues = new float[nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk];
        bufferDensityValues.GetData(densityValues);
        return densityValues;
    }

    public void ReleaseBuffers()
    {
        bufferDensityValues.Release();
    }
}
