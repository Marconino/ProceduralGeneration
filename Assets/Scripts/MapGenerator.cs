using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    static MapGenerator instance;
    public static MapGenerator Instance { get => instance; }

    [Header("Map Parameters")]
    [SerializeField, Range(1, 5)] int LOD = 1;
    [SerializeField, Min(2), Tooltip("Multiple of 8")] int nbPointsPerChunk = 8;
    [SerializeField] Material material;

    enum Directions
    {
        Right, Left, Up, Down, Forward, Back, Count
    }

    struct DirectionsOffset
    {
        public Vector3Int[] offsets;
    }

    public struct Positions
    {
        public Vector3Int gridPos;
        public Vector3 worldPos;

        public Positions(Vector3Int _gridPos, Vector3 _worldPos)
        {
            gridPos = _gridPos;
            worldPos = _worldPos;
        }

        public void Set(Vector3Int _gridPos, Vector3 _worldPos)
        {
            gridPos = _gridPos;
            worldPos = _worldPos;
        }
    }

    [Serializable]
    class Viewer
    {
        public GameObject go;
        public int viewDistance;
        [HideInInspector] public Positions viewerPos;
        [HideInInspector] public Positions lastViewerPos;
        [HideInInspector] public DirectionsOffset[] offsetDirections;
    }

    [Header("Viewers Parameters")]
    [SerializeField] Viewer[] viewers;

    Dictionary<Positions, Chunk> chunks;

    //Cached Variables
    Vector3Int currChunkGridPos;
    Vector3 currChunkWorldPos;
    Positions currChunkPos;
    Chunk currChunk;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        chunks = new Dictionary<Positions, Chunk>();
        currChunkGridPos = Vector3Int.zero;
        currChunkWorldPos = Vector3.zero;
        currChunkPos = new Positions(currChunkGridPos, currChunkWorldPos);
        currChunk = null;

        MarchingCubes.Instance.SetNbPointsPerChunk(nbPointsPerChunk);
        Noise.Instance.SetNbPointsPerChunk(nbPointsPerChunk);

        InitViewers();
    }
    void Update()
    {
        CheckViewersPositions();
    }
    void InitViewers()
    {
        foreach (Viewer viewer in viewers)
        {
            Positions viewerPos = new Positions(Vector3Int.zero, Vector3.zero);
            viewer.viewerPos = viewerPos;
            viewer.lastViewerPos = viewerPos;
            viewer.offsetDirections = new DirectionsOffset[6];
            int viewDistance = viewer.viewDistance;

            for (int currDirection = 0; currDirection < (int)Directions.Count; currDirection++)
            {
                viewer.offsetDirections[currDirection].offsets = new Vector3Int[(viewDistance * 2 + 1) * (viewDistance * 2 + 1)];
                int index = 0;
                int valueNotChanged = currDirection == (int)Directions.Right
                                   || currDirection == (int)Directions.Up
                                   || currDirection == (int)Directions.Forward ? viewDistance : -viewDistance;

                for (int i = -viewDistance; i <= viewDistance; i++)
                {
                    for (int j = viewDistance; j >= -viewDistance; j--)
                    {
                        if (currDirection <= (int)Directions.Left)
                        {
                            viewer.offsetDirections[currDirection].offsets[index] = new Vector3Int(valueNotChanged, i, j);
                        }
                        else if (currDirection <= (int)Directions.Down)
                        {
                            viewer.offsetDirections[currDirection].offsets[index] = new Vector3Int(-j, valueNotChanged, -i);
                        }
                        else
                        {
                            viewer.offsetDirections[currDirection].offsets[index] = new Vector3Int(j, i, valueNotChanged);
                        }
                        index++;
                    }
                }
            }
            CreateSpawnChunks(viewer);
        }
    }

    void CreateSpawnChunks(Viewer _viewer)
    {
        int viewDistance = _viewer.viewDistance;

        for (int z = -viewDistance; z < viewDistance + 1; z++)
        {
            for (int y = -viewDistance; y < viewDistance + 1; y++)
            {
                for (int x = -viewDistance; x < viewDistance + 1; x++)
                {
                    currChunkGridPos.Set(x + _viewer.viewerPos.gridPos.x, y + _viewer.viewerPos.gridPos.y, z + _viewer.viewerPos.gridPos.z);
                    currChunkWorldPos.Set(
                        (currChunkGridPos.x * nbPointsPerChunk) - currChunkGridPos.x,
                        (currChunkGridPos.y * nbPointsPerChunk) - currChunkGridPos.y,
                        (currChunkGridPos.z * nbPointsPerChunk) - currChunkGridPos.z);

                    currChunkPos.Set(currChunkGridPos, currChunkWorldPos);
                    currChunk = CreateNewChunk(currChunkPos);
                    currChunk.SetActive(true);
                }
            }
        }
    }

    void CheckViewersPositions()
    {
        foreach (Viewer viewer in viewers)
        {
            viewer.viewerPos = GetChunkPosWithWorldPos(viewer.go.transform.position);

            if (viewer.viewerPos.gridPos != viewer.lastViewerPos.gridPos)
            {
                LoadAndUnloadChunks(viewer);
            }
        }
    }

    void LoadAndUnloadChunks(Viewer _viewer)
    {
        Vector3Int result = _viewer.viewerPos.gridPos - _viewer.lastViewerPos.gridPos;
        List<Directions> directions = new List<Directions>();

        GetDirections(result, ref directions);
        for (int i = 0; i < directions.Count; i++)
        {
            Directions direction = directions[i];

            UpdateChunks(_viewer.offsetDirections[(int)direction].offsets, _viewer.viewerPos.gridPos);

            //Unload out of range chunks
            switch (direction)
            {
                case Directions.Right: direction = Directions.Left; break;
                case Directions.Left: direction = Directions.Right; break;
                case Directions.Up: direction = Directions.Down; break;
                case Directions.Down: direction = Directions.Up; break;
                case Directions.Forward: direction = Directions.Back; break;
                case Directions.Back: direction = Directions.Forward; break;

            }

            UpdateChunks(_viewer.offsetDirections[(int)direction].offsets, _viewer.lastViewerPos.gridPos, false);
        }
        _viewer.lastViewerPos.gridPos = _viewer.viewerPos.gridPos;
    }

    void UpdateChunks(Vector3Int[] _offsets, Vector3Int _viewerPos, bool _setActive = true)
    {
        foreach (Vector3Int offset in _offsets)
        {
            currChunkGridPos.Set(_viewerPos.x + offset.x, _viewerPos.y + offset.y, _viewerPos.z + offset.z);
            currChunkWorldPos.Set(
                (currChunkGridPos.x * nbPointsPerChunk) - currChunkGridPos.x,
                (currChunkGridPos.y * nbPointsPerChunk) - currChunkGridPos.y,
                (currChunkGridPos.z * nbPointsPerChunk) - currChunkGridPos.z);

            currChunkPos.Set(currChunkGridPos, currChunkWorldPos);

            bool hasChunk = chunks.TryGetValue(currChunkPos, out currChunk);

            if (!hasChunk)
            {
                CreateNewChunk(currChunkPos);
            }
            currChunk.SetActive(_setActive);
        }
    }

    void GetDirections(Vector3Int _directionsResult, ref List<Directions> _directions)
    {
        for (int i = 0; i < 3; i++)
        {
            Directions currDirection = i == 0 ? Directions.Right :
                                       i == 1 ? Directions.Up : Directions.Forward;

            if (_directionsResult[i] != 0)
            {
                _directions.Add(_directionsResult[i] < 0 ? currDirection + 1 : currDirection);
            }
        }
    }

    Chunk CreateNewChunk(Positions _chunkPositions)
    {
        currChunk = new Chunk("Chunk " + chunks.Count, material);
        currChunk.SetParent(transform);
        currChunk.CreateChunk(_chunkPositions);
        chunks.Add(_chunkPositions, currChunk);
        return currChunk;
    }

    public void GetChunkWithWorldPos(Vector3 _worldPos, out Chunk _chunk)
    {
        Positions chunkPos = GetChunkPosWithWorldPos(_worldPos);
        chunks.TryGetValue(chunkPos, out _chunk);
    }

    Positions GetChunkPosWithWorldPos(Vector3 pos)
    {
        float middleDist = (nbPointsPerChunk - 1) / 2f;
        return chunks.Values.First(c => c.Contains(pos, middleDist)).GetPositions();
    }
}
