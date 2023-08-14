using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

//public struct Vertices
//{
//    public Vector3 pos;
//    public float density;
//    public bool isActived;
//}

//public struct Cube
//{
//    public Vector3 pos;
//    public Vertices[] vertices;
//    public int[] triangles;
//    public Vector3[] trianglesVertices;
//}

public class MapGenerator : MonoBehaviour
{
    static MapGenerator instance;
    public static MapGenerator Instance { get => instance; }

    [Header("Map Parameters")]
    [SerializeField, Min(1)] int nbChunksX = 1;
    [SerializeField, Min(1)] int nbChunksY = 1;
    [SerializeField, Min(1)] int nbChunksZ = 1;
    [SerializeField, Min(2), Tooltip("Multiple of 8")] int nbPointsPerChunk = 8;
    [SerializeField] Material material;

    Chunk[] chunks;
    void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        chunks = new Chunk[nbChunksX * nbChunksY * nbChunksZ];

        for (int z = 0; z < nbChunksZ; z++)
        {
            for (int y = 0; y < nbChunksY; y++)
            {
                for (int x = 0; x < nbChunksX; x++)
                {
                    int index = x + nbChunksX * (y + nbChunksY * z);
                    chunks[index] = new Chunk("Chunk " + index, material);
                    chunks[index].SetParent(transform);
                    chunks[index].CreateChunk(nbPointsPerChunk, x, y, z);  
                }
            }
        }     
    }
}
