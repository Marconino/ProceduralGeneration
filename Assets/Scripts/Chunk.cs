using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    MapParameters.Positions pos;
    Mesh cachedMesh;
    PooledGameObject currentPooledGO;
    
    public void SetPos(MapParameters.Positions _pos)
    {
        pos = _pos;
    }
    public MapParameters.Positions GetPos()
    {
        return pos;
    }

    public void SetCachedMesh(Mesh _mesh)
    {
        cachedMesh = _mesh;
    }

    public Mesh GetCachedMesh()
    {
        return cachedMesh;
    }

    public bool HasCachedMesh()
    {
        return cachedMesh != null;
    }

    public void SetCurrentPooledGameObject(PooledGameObject _currentPooledGO)
    {
        currentPooledGO = _currentPooledGO;
    }

    public PooledGameObject GetCurrentPooledGameObject()
    {
        return currentPooledGO;
    }
}
