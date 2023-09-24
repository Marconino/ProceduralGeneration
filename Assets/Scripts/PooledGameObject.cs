using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PooledGameObject
{
    MeshFilter filter;
    MeshCollider collider;
    MeshRenderer renderer;
    MapParameters.Positions pos;
    GameObject go;
    bool isUsed;

    public PooledGameObject(GameObject _go, MapParameters.Positions _pos, MeshFilter _filter, MeshCollider _collider, MeshRenderer _renderer , Transform _parent)
    {
        go = _go;
        pos = _pos;
        filter = _filter;
        collider = _collider;
        renderer = _renderer;

        go.transform.SetParent(_parent);
        go.transform.position = pos.world;
        isUsed = true;
    }

    public void SetCurrentMesh(Mesh _mesh)
    {
        filter.sharedMesh = _mesh;
        collider.sharedMesh = _mesh;
    }

    public void SetPositions(MapParameters.Positions _pos)
    {
        pos = _pos;
        go.transform.position = pos.world;
    }

    public MapParameters.Positions GetPositions()
    {
        return pos;
    }

    public bool GetState()
    {
        return isUsed;
    }
    public void SetState(bool _state)
    {
        isUsed = _state;
    }
}
