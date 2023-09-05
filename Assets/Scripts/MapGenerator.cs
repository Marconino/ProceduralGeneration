using System;
using System.Collections.Generic;
using System.Linq;
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
        Left, Right, Up, Down
    }

    struct DirectionsOffset
    {
        public Vector2Int[] offsets;
    }

    struct Positions
    {
        public Vector2Int gridPos;
        public Vector3 worldPos;

        public Positions(Vector2Int _gridPos, Vector3 _worldPos)
        {
            gridPos = _gridPos;
            worldPos = _worldPos;
        }

        public void Set(Vector2Int _gridPos, Vector3 _worldPos)
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
    Vector2Int currChunkGridPos;
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
        currChunkGridPos = Vector2Int.zero;
        currChunkWorldPos = Vector3.zero;
        currChunkPos = new Positions(currChunkGridPos, currChunkWorldPos);
        currChunk = null;

        MarchingCubes.Instance.SetNbPointsPerChunk(nbPointsPerChunk);
        Noise.Instance.SetNbPointsPerChunk(nbPointsPerChunk);
        InitViewers();
    }

    void InitViewers()
    {
        foreach (Viewer viewer in viewers)
        {
            Positions viewerPos = new Positions(Vector2Int.zero, Vector3.zero);
            viewer.viewerPos = viewerPos;
            viewer.lastViewerPos = viewerPos;
            viewer.offsetDirections = new DirectionsOffset[4];
            int viewDistance = viewer.viewDistance;

            int index = 0;

            viewer.offsetDirections[0].offsets = new Vector2Int[viewDistance * 2 + 1];
            viewer.offsetDirections[1].offsets = new Vector2Int[viewDistance * 2 + 1];
            viewer.offsetDirections[2].offsets = new Vector2Int[viewDistance * 2 + 1];
            viewer.offsetDirections[3].offsets = new Vector2Int[viewDistance * 2 + 1];

            for (int j = -viewDistance; j < viewDistance + 1; j++)
            {

                viewer.offsetDirections[0].offsets[index] = new Vector2Int(-viewDistance, j); //Left
                viewer.offsetDirections[1].offsets[index] = new Vector2Int(viewDistance, j);  //Right
                viewer.offsetDirections[2].offsets[index] = new Vector2Int(j, viewDistance);  //Down
                viewer.offsetDirections[3].offsets[index] = new Vector2Int(j, -viewDistance); //Up

                index++;
            }

            CreateSpawnChunks(viewer);
        }
    }

    void CreateSpawnChunks(Viewer _viewer)
    {
        int viewDistance = _viewer.viewDistance;

        for (int z = -viewDistance; z < viewDistance + 1; z++)
        {
            for (int x = -viewDistance; x < viewDistance + 1; x++)
            {
                currChunkGridPos.Set(x + _viewer.viewerPos.gridPos.x, z + _viewer.viewerPos.gridPos.y);
                currChunkWorldPos.Set(
                    (currChunkGridPos.x * nbPointsPerChunk) - currChunkGridPos.x,
                    0,
                    (currChunkGridPos.y * nbPointsPerChunk) - currChunkGridPos.y);

                currChunkPos.Set(currChunkGridPos, currChunkWorldPos);
                currChunk = CreateNewChunk(currChunkPos);
                currChunk.SetActive(true);
            }
        }
    }

    void UpdateChunks(Vector2Int[] _offsets, Vector2Int _viewerPos, bool _setActive = true)
    {
        foreach (Vector2Int offset in _offsets)
        {
            currChunkGridPos.Set(_viewerPos.x + offset.x, _viewerPos.y + offset.y);
            currChunkWorldPos.Set(
                (currChunkGridPos.x * nbPointsPerChunk) - currChunkGridPos.x,
                0,
                (currChunkGridPos.y * nbPointsPerChunk) - currChunkGridPos.y);

            currChunkPos.Set(currChunkGridPos, currChunkWorldPos);

            bool hasChunk = chunks.TryGetValue(currChunkPos, out currChunk);

            if (!hasChunk)
            {
                CreateNewChunk(currChunkPos);
            }
            currChunk.SetActive(_setActive);
        }
    }

    void Update()
    {
        CheckViewersPositions();
    }

    void CheckViewersPositions()
    {
        foreach (Viewer viewer in viewers)
        {
            viewer.viewerPos = GetChunkPosWithWorldPos(viewer.go.transform.position);

            if (viewer.viewerPos.gridPos != viewer.lastViewerPos.gridPos)
            {
                Vector2Int viewerPos = viewer.viewerPos.gridPos;
                Vector2Int result = viewer.viewerPos.gridPos - viewer.lastViewerPos.gridPos;
                Directions direction = result.x < 0 ? Directions.Left : result.y > 0 ? Directions.Up : result.x > 0 ? Directions.Right : Directions.Down;

                UpdateChunks(viewer.offsetDirections[(int)direction].offsets, viewerPos);

                Directions inverseDirection;
                //Inverse
                switch (direction)
                {
                    case Directions.Left:  inverseDirection = Directions.Right; viewerPos.x += 1; break;
                    case Directions.Right: inverseDirection = Directions.Left;  viewerPos.x -= 1; break;
                    case Directions.Up:    inverseDirection = Directions.Down;  viewerPos.y -= 1; break;
                    case Directions.Down:  inverseDirection = Directions.Up;    viewerPos.y += 1; break;
                    default:               inverseDirection = Directions.Left;  break;
                }

                UpdateChunks(viewer.offsetDirections[(int)inverseDirection].offsets, viewerPos, false);

                viewer.lastViewerPos.gridPos = viewer.viewerPos.gridPos;
            }
        }
    }

    Chunk CreateNewChunk(Positions _chunkPositions)
    {
        currChunk = new Chunk("Chunk " + chunks.Count, material);
        currChunk.SetParent(transform);
        currChunk.CreateChunk(_chunkPositions.worldPos);
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

        Positions chunkPos = chunks.Keys.First(key =>
        {
            float minX = key.worldPos.x - middleDist;
            float maxX = key.worldPos.x + middleDist;
            float minZ = key.worldPos.z - middleDist;
            float maxZ = key.worldPos.z + middleDist;

            return pos.x >= minX && pos.x <= maxX && pos.z >= minZ && pos.z <= maxZ;
        });
        return chunkPos;
    }

    //private void OnDrawGizmos()
    //{

    //    foreach (var chunk in chunks)
    //    {
    //        chunk.Value.DisplayNoise(nbPointsPerChunk);
    //    }
    //}
}
