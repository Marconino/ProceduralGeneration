using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    static MapGenerator instance;
    public static MapGenerator Instance { get => instance; }

    [Header("Map Parameters")]
    [SerializeField] Vector2Int HeightLimit;
    [SerializeField] Material material;
    [SerializeField] Viewer viewer;
    float[] LODThresholds;

    Dictionary<MapParameters.Positions, Chunk> chunks;

    //Cached Variables
    Vector3Int currChunkGridPos;
    Vector3 currChunkWorldPos;
    MapParameters.Positions currChunkPos;

    //Pooling chunks
    PooledGameObject[] pooledGameObjects;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        chunks = new Dictionary<MapParameters.Positions, Chunk>();
        currChunkGridPos = Vector3Int.zero;
        currChunkWorldPos = Vector3.zero;
        currChunkPos = new MapParameters.Positions(currChunkGridPos, currChunkWorldPos);

        LODThresholds = new float[]
        {
            0.5f, 0.85f
        };

        int nbGOPerAxis = viewer.GetViewDistance() * 2 + 1;
        pooledGameObjects = new PooledGameObject[nbGOPerAxis * nbGOPerAxis * nbGOPerAxis];

        InitSpawn();
    }
    void Update()
    {
        CheckViewerPositions();
    }

    void InitSpawn()
    {
        int viewDistance = viewer.GetViewDistance();

        int goCount = 0;
        for (int z = -viewDistance; z <= viewDistance; z++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                for (int x = -viewDistance; x <= viewDistance; x++)
                {
                    currChunkGridPos.Set(x, y, z);
                    SetGridPosToWorldPos(currChunkGridPos);
                    currChunkPos.Set(currChunkGridPos, currChunkWorldPos);

                    GameObject go = new GameObject("GO_pooled_" + goCount);
                    MeshFilter filter = go.AddComponent<MeshFilter>();
                    MeshCollider collider = go.AddComponent<MeshCollider>();
                    MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                    renderer.material = material;

                    PooledGameObject pooledGameObject = new PooledGameObject(go, currChunkPos, filter, collider, renderer, transform);
                    pooledGameObjects[goCount++] = pooledGameObject; 

                    int distance = GetDistanceWithChunk(currChunkGridPos);
                    int lod = GetLODWithGridPos(distance);
                    AddChunk(currChunkPos);
                    SetChunkMeshOnPooledGameObject(pooledGameObject, lod);
                }
            }
        }
    }

    void CheckViewerPositions()
    {
        Vector3Int chunkGridPos = GetChunkPos(viewer.GetCurrentWorldPosition()).grid;
        viewer.SetCurrentGridPos(chunkGridPos);

        if (viewer.GetCurrentGridPosition() != viewer.GetLastGridPosition())
        {
            LoadAndUnloadChunks();
            UpdateLODs();
        }
    }

    void LoadAndUnloadChunks()
    {
        Vector3Int result = viewer.GetCurrentGridPosition() - viewer.GetLastGridPosition();
        List<MapParameters.Directions> directions = new List<MapParameters.Directions>();

        GetDirections(result, ref directions);
        for (int i = 0; i < directions.Count; i++)
        {
            MapParameters.Directions direction = directions[i];

            bool isPositive = (int)direction % 2 == 0 ? true : false;
            MapParameters.Directions inverseDirection = isPositive ? direction + 1 : direction - 1;

            ClearPooledOldChunks(inverseDirection);
            LoadChunks(direction);
        }
        viewer.SetLastGridPos(viewer.GetCurrentGridPosition());
    }

    void ClearPooledOldChunks(MapParameters.Directions _inverseDirection)
    {
        Vector3Int currentGridPos = viewer.GetLastGridPosition();

        foreach (Vector3Int offset in viewer.GetOffsetDirection(_inverseDirection))
        {
            currChunkGridPos.Set(currentGridPos.x + offset.x,
                                 currentGridPos.y + offset.y,
                                 currentGridPos.z + offset.z);
            SetGridPosToWorldPos(currChunkGridPos);
            currChunkPos.Set(currChunkGridPos, currChunkWorldPos);

            PooledGameObject pooledGameObject = pooledGameObjects.First(g => g.GetPositions().grid == currChunkGridPos);
            pooledGameObject.SetState(false);
        }
    }

    void LoadChunks(MapParameters.Directions _direction)
    {
        Vector3Int currentGridPos = viewer.GetCurrentGridPosition();

        foreach (Vector3Int offset in viewer.GetOffsetDirection(_direction))
        {
            currChunkGridPos.Set(currentGridPos.x + offset.x,
                     currentGridPos.y + offset.y,
                     currentGridPos.z + offset.z);
            SetGridPosToWorldPos(currChunkGridPos);
            currChunkPos.Set(currChunkGridPos, currChunkWorldPos);

            Chunk chunk;
            bool hasChunk = chunks.TryGetValue(currChunkPos, out chunk);
            if (!hasChunk)
            {
                chunk = AddChunk(currChunkPos);
            }

            PooledGameObject pooledGameObject = pooledGameObjects.First(g => !g.GetState());
            pooledGameObject.SetPositions(currChunkPos);  
            pooledGameObject.SetCurrentMesh(chunk.GetCurrentMesh());
            pooledGameObject.SetState(true);
        }
    }

    void UpdateLODs()
    {
        foreach (PooledGameObject pooledGameObject in pooledGameObjects)
        {
            int distance = GetDistanceWithChunk(pooledGameObject.GetPositions().grid);
            int lod = GetLODWithGridPos(distance);
            SetChunkMeshOnPooledGameObject(pooledGameObject, lod);
        }
    }

    int GetLODWithGridPos(int _distance)
    {
        float weight = _distance / (float)viewer.GetViewDistance();

        int lod = 0;
        for (int i = 0; i < LODThresholds.Length; i++)
        {
            if (weight <= LODThresholds[i])
            {
                lod = LODThresholds.Length - i;
                break;
            }
        }
        return lod;
    }

    int GetDistanceWithChunk(Vector3Int _chunkGridPos)
    {
        Vector3Int distanceVec = viewer.GetCurrentGridPosition() - _chunkGridPos;
        for (int i = 0; i < 3; i++)
        {
            if (distanceVec[i] < 0)
                distanceVec[i] = Mathf.Abs(distanceVec[i]);
        }

        return Mathf.Max(distanceVec.x, distanceVec.y, distanceVec.z);
    }

    int GetDistanceWithChunk(Vector3Int _chunkGridPos, MapParameters.Directions _direction)
    {
        bool isPositive = (int)_direction % 2 == 0 ? true : false;
        int currentAxis = isPositive ? (int)_direction / 2 : ((int)_direction - 1) / 2;

        return Mathf.Abs(viewer.GetCurrentGridPosition()[currentAxis] - _chunkGridPos[currentAxis]);
    }

    void GetDirections(Vector3Int _directionsResult, ref List<MapParameters.Directions> _directions)
    {
        for (int i = 0; i < 3; i++)
        {
            MapParameters.Directions currDirection = i == 0 ? MapParameters.Directions.Right :
                                                     i == 1 ? MapParameters.Directions.Up : 
                                                     MapParameters.Directions.Forward;

            if (_directionsResult[i] != 0)
            {
                _directions.Add(_directionsResult[i] < 0 ? currDirection + 1 : currDirection);
            }
        }
    }

    Chunk AddChunk(MapParameters.Positions _chunkPos)
    {
        Chunk newChunk = new Chunk();
        newChunk.Init(_chunkPos);
        chunks.Add(_chunkPos, newChunk);
        return newChunk;
    }

    void SetChunkMeshOnPooledGameObject(PooledGameObject _pooledGameObject, int _lod)
    {
        MapParameters.Positions pos = _pooledGameObject.GetPositions();

        Chunk chunk;
        chunks.TryGetValue(pos, out chunk);
        chunk.SetLOD(0);

        _pooledGameObject.SetCurrentMesh(chunk.GetCurrentMesh());
    }

    void SetGridPosToWorldPos(Vector3Int _gridPos)
    {
        currChunkWorldPos.Set((_gridPos.x * MapParameters.GetChunkAxisSize()),
                        (_gridPos.y * MapParameters.GetChunkAxisSize()),
                        (_gridPos.z * MapParameters.GetChunkAxisSize()));
    }

    MapParameters.Positions GetChunkPos(Vector3 _worldPos)
    {
        Chunk chunk = null;
        GetChunkWithWorldPos(_worldPos, out chunk);
        return chunk.GetPositions();
    }

    public void GetChunkWithWorldPos(Vector3 _worldPos, out Chunk _chunk)
    {
        _chunk = chunks.Values.DefaultIfEmpty(null).FirstOrDefault(c => c.Contains(_worldPos));
    }

    public void GetChunksWithWorldPos(Bounds _bounds, out Chunk[] _chunks)
    {
        _chunks = chunks.Values.Where(c => c.Intersects(_bounds)).ToArray();
    }

    private void OnDrawGizmos()
    {
        foreach (PooledGameObject pooledGameObject in pooledGameObjects)
        {
            //Vector3 offset = new Vector3(MapParameters.ChunkSize / 2f, MapParameters.ChunkSize / 2f, MapParameters.ChunkSize / 2f);
            //Vector3 center = chunk.GetPositions().worldPos;

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center + new Vector3(MapParameters.ChunkSize / 2f, 0, 0));
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center - new Vector3(MapParameters.ChunkSize / 2f, 0, 0));
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center + new Vector3(0, 0, MapParameters.ChunkSize / 2f));
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center - new Vector3(0, 0, MapParameters.ChunkSize / 2f));
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center + new Vector3(0, MapParameters.ChunkSize / 2f, 0));
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(center, center - new Vector3(0, MapParameters.ChunkSize / 2f, 0));
            Chunk chunk;
            chunks.TryGetValue(pooledGameObject.GetPositions(), out chunk);
            if (chunk.GetLOD() == 0 && Input.GetKey(KeyCode.Keypad0))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(chunk.chunkBounds.center, chunk.chunkBounds.size);
            }
            else if (chunk.GetLOD() == 1 && Input.GetKey(KeyCode.Keypad1))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(chunk.chunkBounds.center, chunk.chunkBounds.size);
            }
            else if (chunk.GetLOD() == 2 && Input.GetKey(KeyCode.Keypad2))
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(chunk.chunkBounds.center, chunk.chunkBounds.size);
            }
        }
    }
}
