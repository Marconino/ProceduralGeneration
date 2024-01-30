using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MapParameters
{
    const int nbThreads = 8;
    const int chunkAxisSize = 1000;
    readonly static Vector3 chunkSize = new Vector3(chunkAxisSize, chunkAxisSize, chunkAxisSize);
    readonly static int[] LODs =
    {
        8,
        16,
        24,
        32
    };

    public static int GetLODCount()
    {
        return LODs.Length;
    }

    public static Vector3 GetChunkSize()
    {
        return chunkSize;
    }

    public static int GetChunkAxisSize()
    {
        return chunkAxisSize;
    }

    public static int GetPointsPerChunk(int _currentLod)
    {
        return LODs[_currentLod];
    }

    public static int ThreadGroupsPerChunk(int _currentLod)
    {
        return LODs[_currentLod] / nbThreads;
    }

    public enum Directions
    {
        Right, Left, Up, Down, Forward, Back, Count
    }

    public struct Positions
    {
        public Vector3Int grid;
        public Vector3 world;

        public Positions(Vector3Int _grid, Vector3 _world)
        {
            grid = _grid;
            world = _world;
        }

        public void Set(Vector3Int _grid)
        {
            grid = _grid;

            int chunkSizeMax = GetChunkAxisSize();
            world.Set((grid.x * chunkSizeMax),
                      (grid.y * chunkSizeMax),
                      (grid.z * chunkSizeMax));
        }
    }
}
