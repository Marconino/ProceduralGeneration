using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    GameObject go;
    Mesh mesh;
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;
    MapGenerator.Positions pos;
    float[] densityValues;

    public Chunk(string _name, Material _mat)
    {
        go = new GameObject(_name);
        mesh = new Mesh();
        filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();
        collider = go.AddComponent<MeshCollider>();

        filter.sharedMesh = mesh;
        renderer.material = _mat;
        collider.sharedMesh = mesh;

        renderer.enabled = false;
        collider.enabled = false;
        go.SetActive(false);
    }
    public Mesh GetMesh()
    {
        return mesh;
    }
    public void CreateChunk(MapGenerator.Positions _pos)
    {
        pos = _pos;
        go.transform.position = pos.worldPos;

        densityValues = Noise.Instance.Compute((int)pos.worldPos.x, (int)pos.worldPos.y, (int)pos.worldPos.z);
        MarchingCubes.Instance.Compute(ref mesh, (int)pos.worldPos.x, (int)pos.worldPos.y, (int)pos.worldPos.z, densityValues);
    }

    public void Edit(Vector3 _hitPos, float _radiusTerraforming, bool _isConstruct, float _strengh)
    {
        MarchingCubes.Instance.Edit(densityValues, _hitPos, pos.worldPos, _radiusTerraforming, _isConstruct, _strengh);
        MarchingCubes.Instance.Compute(ref mesh, (int)pos.worldPos.x, (int)pos.worldPos.y, (int)pos.worldPos.z, densityValues);

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
    }

    public void SetParent(Transform _parent)
    {
        go.transform.SetParent(_parent);
    }

    public void SetActive(bool _state)
    {
        go.SetActive(_state);
        collider.enabled = _state;
        renderer.enabled = _state;
    }

    public MapGenerator.Positions GetPositions()
    {
        return pos;
    }

    public bool Contains(Vector3 _pos, float _middleDist)
    {
        return pos.worldPos.x - _middleDist < _pos.x &&
            pos.worldPos.x + _middleDist > _pos.x &&
            pos.worldPos.y - _middleDist < _pos.y &&
            pos.worldPos.y + _middleDist > _pos.y &&
            pos.worldPos.z - _middleDist < _pos.z &&
            pos.worldPos.z + _middleDist > _pos.z;
    }
}
