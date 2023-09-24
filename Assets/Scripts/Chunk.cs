using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    Mesh[] meshes;
    MapParameters.Positions pos;
    public Bounds chunkBounds;
    float[][] LODValues;
    int currentLOD;

    public Chunk()
    {
        LODValues = new float[MapParameters.GetLODCount()][];
        meshes = new Mesh[MapParameters.GetLODCount()];
    }

    public void Init(MapParameters.Positions _pos)
    {
        pos = _pos;
        chunkBounds = new Bounds(pos.world, MapParameters.GetChunkSize());
    }

    void ComputeNoise()
    {
        int nbPoints = MapParameters.GetPointsPerChunk(currentLOD) - 1;
        LODValues[currentLOD] = Noise.Instance.Compute(pos.grid.x * nbPoints, pos.grid.y * nbPoints, pos.grid.z * nbPoints, currentLOD);
    }
    void ComputeMarchingCubes()
    {
        meshes[currentLOD] = MarchingCubes.Instance.Compute(LODValues[currentLOD], currentLOD);
    }

    public void Edit(Vector3 _hitPos, float _radiusTerraforming, bool _isConstruct, float _strengh)
    {
        MarchingCubes.Instance.Edit(LODValues[currentLOD], _hitPos, pos.world, _radiusTerraforming, _isConstruct, _strengh, currentLOD);
        meshes[currentLOD] = MarchingCubes.Instance.Compute(LODValues[currentLOD], currentLOD, true);

        //filter.sharedMesh = meshes[currentLOD];
        //collider.sharedMesh = meshes[currentLOD];
    }

    public void SetLOD(int _lod)
    {
        currentLOD = _lod;

        if (meshes[currentLOD] == null)
        {
            ComputeNoise();
            ComputeMarchingCubes();
        }

        //collider.sharedMesh = meshes[currentLOD];
        //filter.sharedMesh = meshes[currentLOD];
    }

    public Mesh GetCurrentMesh()
    {
        return meshes[currentLOD];
    }

    public int GetLOD()
    {
        return currentLOD;
    }

    public MapParameters.Positions GetPositions()
    {
        return pos;
    }

    public bool Contains(Vector3 _pos)
    {
        return chunkBounds.Contains(_pos);
    }

    public bool Intersects(Bounds _bounds)
    {
        return chunkBounds.Intersects(_bounds);

        //return pos.worldPos.x - _middleDist < _pos.x &&
        //    pos.worldPos.x + _middleDist > _pos.x &&
        //    pos.worldPos.y - _middleDist < _pos.y &&
        //    pos.worldPos.y + _middleDist > _pos.y &&
        //    pos.worldPos.z - _middleDist < _pos.z &&
        //    pos.worldPos.z + _middleDist > _pos.z;
    }
}
