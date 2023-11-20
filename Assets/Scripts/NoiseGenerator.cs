using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class NoiseGenerator : MonoBehaviour
{
    public class Noise : BaseComputeShader<NativeArray<float>>
    {
        DataCompute<float> density;
        int xOffset, yOffset, zOffset;

        public Noise(int _lod, int _x, int _y, int _z) : base(_lod, _x, _y, _z)
        {
            xOffset = gridPosX * (nbPointsPerChunk - 1); 
            yOffset = gridPosY * (nbPointsPerChunk - 1);
            zOffset = gridPosZ * (nbPointsPerChunk - 1);
        }

        protected override void CreateDataCompute()
        {
            density = new DataCompute<float>();
        }

        protected override void InitBuffers()
        {
            int allPointsPerChunk = nbPointsPerChunk * nbPointsPerChunk * nbPointsPerChunk;
            density.computeBuffer = new ComputeBuffer(allPointsPerChunk, sizeof(float));
        }

        protected override void ReleaseBuffers()
        {
            density.computeBuffer.Release();
        }

        protected override void StartRequest()
        {
            ComputeShader noiseShader = instance.noiseShader;
            int kernel = noiseShader.FindKernel("GenerateNoise");

            noiseShader.SetBuffer(kernel, "_DensityValues", density.computeBuffer);

            noiseShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
            noiseShader.SetInt("_ThreadGroups", threadGroups);
            noiseShader.SetInt("_Octaves", instance.octaves);
            noiseShader.SetInt("_Seed", instance.seed);
            noiseShader.SetFloat("_Amplitude", instance.amplitude);
            noiseShader.SetFloat("_Frequency", instance.frequency);
            noiseShader.SetFloat("_Lacunarity", instance.lacunarity);
            noiseShader.SetFloat("_GroundOffset", instance.groundOffset);
            noiseShader.SetFloat("_OffsetX", xOffset);
            noiseShader.SetFloat("_OffsetY", yOffset);
            noiseShader.SetFloat("_OffsetZ", zOffset);

            noiseShader.Dispatch(kernel, threadGroups, threadGroups, threadGroups);

            AsyncGPUReadback.Request(density.computeBuffer, density.OnReadbackData);
        }

        public override NativeArray<float> GetComputeAsync()
        {
            return density.data;
        }

        public override bool IsComputed()
        {
            return density.data.IsCreated;
        }

        public override void DisposeData()
        {
            density.data.Dispose();
        }
    }

    static NoiseGenerator instance;
    public static NoiseGenerator Instance { get => instance; }

    [Header("Noise Parameters")]
    [SerializeField] ComputeShader noiseShader;
    [SerializeField] int seed;
    [SerializeField, Min(1)] int octaves = 1;
    [SerializeField] float amplitude = 5f;
    [SerializeField] float frequency = 0.005f;
    [SerializeField] float lacunarity = 2f;
    [SerializeField] float groundOffset = 0.2f;
    Queue<Noise> noises;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            noises = new Queue<Noise>();
        }
    }

    public Noise CreateNoiseInstance(int _lod, int _x, int _y, int _z)
    {
        Noise noiseInstance = new Noise(_lod, _x, _y, _z);
        noises.Enqueue(noiseInstance);
        return noiseInstance;
    }

    public Noise DequeueNoiseComputed()
    {
        return noises.Dequeue();
    }

    public bool HasAComputedNoise()
    {
        return noises.Count > 0 && noises.Peek().IsComputed();
    }
}
