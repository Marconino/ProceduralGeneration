using UnityEngine;

public class Chunk
{
    GameObject go;
    Mesh mesh;
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;
    Vector3 pos;
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
    public void CreateChunk(Vector3 _pos)
    {
        pos = go.transform.position = _pos;

        densityValues = Noise.Instance.Compute((int)_pos.x, (int)_pos.y, (int)_pos.z);
        MarchingCubes.Instance.Compute(ref mesh, (int)_pos.x, (int)_pos.y, (int)_pos.z, densityValues);
    }

    public void Edit(Vector3 _hitPos, float _radiusTerraforming, bool _isConstruct)
    {
        MarchingCubes.Instance.Edit(densityValues, _hitPos, pos, _radiusTerraforming, _isConstruct);
        MarchingCubes.Instance.Compute(ref mesh, (int)pos.x, (int)pos.y, (int)pos.z, densityValues);

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

    public Vector3 GetPos()
    {
        return pos;
    }

    //public void DisplayNoise(int _nbPointsPerChunk)
    //{
    //    if (densityValues == null || densityValues.Length == 0)
    //    {
    //        return;
    //    }
    //    for (int x = 0; x < _nbPointsPerChunk; x++)
    //    {
    //        for (int y = 0; y < _nbPointsPerChunk; y++)
    //        {
    //            for (int z = 0; z < _nbPointsPerChunk; z++)
    //            {
    //                int index = x + _nbPointsPerChunk * (y + _nbPointsPerChunk * z);
    //                float noiseValue = densityValues[index];
    //                Gizmos.color = Color.Lerp(Color.black, Color.white, noiseValue);
    //                Gizmos.DrawCube(pos + new Vector3(x - _nbPointsPerChunk / 2f, y - _nbPointsPerChunk / 2f, z - _nbPointsPerChunk / 2f) , Vector3.one * .2f);
    //            }
    //        }
    //    }
    //}
}
