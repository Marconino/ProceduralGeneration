using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    Mesh mesh;

    public Mesh GetCurrentMesh()
    {
        return mesh;
    }   
    public void SetCurrentMesh(Mesh _mesh)
    {
        mesh = _mesh;
    }
}
