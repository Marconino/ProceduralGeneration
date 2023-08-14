using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    GameObject go;
    Mesh mesh;
    MeshFilter filter;
    MeshRenderer renderer;
    MeshCollider collider;

    public Chunk(string _name, Material _mat)
    {
        go = new GameObject(_name);
        mesh = new Mesh();
        filter = go.AddComponent<MeshFilter>();
        renderer = go.AddComponent<MeshRenderer>();
        collider = go.AddComponent<MeshCollider>();

        filter.mesh = mesh;
        renderer.material = _mat;
        collider.sharedMesh = mesh;
    }

    public void CreateChunk(int _nbPointsPerChunk, int _x, int _y, int _z)
    {
        go.transform.position = new Vector3(_x * _nbPointsPerChunk, _y * _nbPointsPerChunk, _z * _nbPointsPerChunk);

        List<int> triangles = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> currVertices = new List<Vector3>();
        int lastMaxVertex = 0;

        Noise.Instance.CreateBuffers(_nbPointsPerChunk);
        float[] densityValues = Noise.Instance.Compute(_x, _y, _z);
        Noise.Instance.ReleaseBuffers();

        float[] currDensityValues = new float[8];
        for (int z = 0; z < _nbPointsPerChunk; z++)
        {
            for (int y = 0; y < _nbPointsPerChunk; y++)
            {
                for (int x = 0; x < _nbPointsPerChunk; x++)
                {
                    if (x + 1 >= _nbPointsPerChunk || y + 1 >= _nbPointsPerChunk || z + 1 >= _nbPointsPerChunk)
                        break;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    int flagIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int nextPos = pos + MarchingCubes.GetVerticesOffset(i);
                        currDensityValues[i] = densityValues[MarchingCubes.IndexFromPos(nextPos.x, nextPos.y, nextPos.z, _nbPointsPerChunk)];

                        if (currDensityValues[i] < 0)
                            flagIndex |= 1 << i;
                    }

                    int[] currTriangles;
                    MarchingCubes.CreateTrianglesAndVertices(flagIndex, pos, currDensityValues, ref currVertices, out currTriangles);
                    for (int i = 0; i < currVertices.Count; i++)
                    {
                        if (vertices.Contains(currVertices[i]))
                        {
                            currTriangles[i] = vertices.IndexOf(currVertices[i]);
                        }
                        else
                        {
                            vertices.Add(currVertices[i]);
                            currTriangles[i] = lastMaxVertex++;
                        }
                    }
                    triangles.AddRange(currTriangles);
                    currVertices.Clear();
                }
            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
    }

    public void SetParent(Transform _parent)
    {
        go.transform.SetParent(_parent);
    }
}
