using UnityEngine;

public class Chunk
{
    GameObject go;
    Mesh mesh;
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;
    Vector3 pos;

    public Chunk(string _name, Material _mat)
    {
        go = new GameObject(_name);
        mesh = new Mesh();
        filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();
        //collider = go.AddComponent<MeshCollider>();

        filter.mesh = mesh;
        renderer.material = _mat;
        //collider.sharedMesh = mesh;

        go.SetActive(false);
    }

    public void CreateChunk(int _nbPointsPerChunk, int _x, int _y, int _z)
    {
        int x = (_x * _nbPointsPerChunk) - _x;
        int y = (_y * _nbPointsPerChunk) - _y;
        int z = (_z * _nbPointsPerChunk) - _z;

        pos = go.transform.position = new Vector3(x, y, z);

        MarchingCubes.Instance.CreateBuffers(_nbPointsPerChunk);
        MarchingCubes.Instance.Compute(ref mesh, x, y, z);
        MarchingCubes.Instance.ReleaseBuffers();
    }

    public void SetParent(Transform _parent)
    {
        go.transform.SetParent(_parent);
    }

    public void SetActive(bool _state)
    {
        go.SetActive(_state);
    }
}
