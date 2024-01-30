using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class NoiseGenerator : MonoBehaviour
{
    public class Noise : BaseComputeShader<NativeArray<float>>
    {
        DataCompute<float> density;
        int xOffset, yOffset, zOffset;

        //Generator
        NoiseGenerator generator;

        public Noise(NoiseGenerator _generator, int _lod, int _x, int _y, int _z) : base(_lod, _x, _y, _z)
        {
            generator = _generator;
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
            density.computeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, allPointsPerChunk, sizeof(float));
        }

        protected override void ReleaseBuffers()
        {
            density.computeBuffer.Release();
        }

        protected override void StartRequest()
        {
            ComputeShader noiseShader = generator.noiseShader;
            int kernel = noiseShader.FindKernel("GenerateNoise");

            noiseShader.SetBuffer(kernel, "_DensityValues", density.computeBuffer);

            noiseShader.SetInt("_nbPointsPerChunk", nbPointsPerChunk);
            noiseShader.SetInt("_ThreadGroups", threadGroups);
            noiseShader.SetInt("_Octaves", generator.octaves);
            noiseShader.SetInt("_Seed", generator.seed);
            noiseShader.SetFloat("_Amplitude", generator.amplitude);
            noiseShader.SetFloat("_Frequency", generator.frequency);
            noiseShader.SetFloat("_Lacunarity", generator.lacunarity);
            noiseShader.SetFloat("_GroundOffset", generator.groundOffset);
            noiseShader.SetFloat("_OffsetX", xOffset);
            noiseShader.SetFloat("_OffsetY", yOffset);
            noiseShader.SetFloat("_OffsetZ", zOffset);
            noiseShader.SetFloat("_Scale", generator.scale);

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

    [Header("Noise Parameters")]
    [SerializeField] ComputeShader noiseShader;
    [SerializeField] int seed;
    [SerializeField, Min(1)] int octaves = 1;
    [SerializeField] float amplitude = 5f;
    [SerializeField] float frequency = 0.005f;
    [SerializeField] float lacunarity = 2f;
    [SerializeField] float groundOffset = 0.2f;
    [SerializeField] float scale = 0.3f;
    Queue<Noise> noises;

    NoiseGenerator()
    {
        noises = new Queue<Noise>();
    }

    private void Update()
    {
        //Debug.Log("Noises count : " + noises.Count);
    }

    public Noise CreateNoiseInstance(int _lod, int _x, int _y, int _z)
    {
        Noise noiseInstance = new Noise(this, _lod, _x, _y, _z);
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

    public bool HasNoises()
    {
        return noises.Count > 0;
    }

    public void ClearOldNoises()
    {
        noises.Clear();
    }
}
