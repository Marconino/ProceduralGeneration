using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk
{
    Mesh mesh;
    MapParameters.Positions pos;
    public Mesh GetCurrentMesh()
    {
        return mesh;
    }   
    public void SetCurrentMesh(Mesh _mesh)
    {
        mesh = _mesh;
    }
    
    public void SetPos(MapParameters.Positions _pos)
    {
        pos = _pos;
    }
    public MapParameters.Positions GetPos()
    {
        return pos;
    }
}
