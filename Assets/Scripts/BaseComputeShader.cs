using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class BaseComputeShader<TResult>
{
    public class DataCompute<TValue> where TValue : struct
    {
        public GraphicsBuffer computeBuffer;
        public NativeArray<TValue> data;

        public void OnReadbackData(AsyncGPUReadbackRequest _request)
        {
            data = new NativeArray<TValue>(_request.GetData<TValue>(), Allocator.Persistent);
        }
    }

    protected int nbPointsPerChunk;
    protected int threadGroups;
    protected int gridPosX, gridPosY, gridPosZ;

    public BaseComputeShader(int _lod, int _x, int _y, int _z)
    {
        CreateDataCompute();

        nbPointsPerChunk = MapParameters.GetPointsPerChunk(_lod);
        threadGroups = MapParameters.ThreadGroupsPerChunk(_lod);

        gridPosX = _x;
        gridPosY = _y;
        gridPosZ = _z;
    }

    protected abstract void CreateDataCompute();
    protected abstract void InitBuffers();
    protected abstract void ReleaseBuffers();
    protected abstract void StartRequest();
    public abstract TResult GetComputeAsync();

    public abstract bool IsComputed();
    public abstract void DisposeData();

    public void StartComputeAsync()
    {
        InitBuffers();
        StartRequest();
        ReleaseBuffers();
    }

    public Vector3Int GetGridPos()
    {
        return new Vector3Int(gridPosX, gridPosY, gridPosZ);
    }
}
