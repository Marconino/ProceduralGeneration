using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
public class MapGenerator : MonoBehaviour
{
    [Serializable]
    public class VertexColorParameters
    {
        public Gradient gradient;
        public float maxHeight;
        public float minHeight;
    }

    [SerializeField] Viewer viewer;
    [SerializeField] Material meshMaterial;
    [SerializeField] VertexColorParameters vertexColorParameters;

    Dictionary<MapParameters.Positions, Chunk> chunks;

    PooledGameObject[] pooledGameObjects;
    Queue<PooledGameObject> unusedGO;

    //Cached variables
    Vector3Int currChunkGridPos;
    MapParameters.Positions currChunkPos;
    List<MapParameters.Directions> currDirections;

    [SerializeField][Range(0, 3)] int resolution;
    RenderParams renderParams;

    NoiseGenerator noiseGenerator;
    MarchingCubesGenerator marchingCubesGenerator;

    [HideInInspector] public bool updateInEditor = false;

    void Start()
    {
        Application.quitting += OnApplicationQuitting;

        if (Application.isPlaying)
        {
            if (transform.childCount > 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
            Generate();
        }
        else if (updateInEditor)
        {
            noiseGenerator = GetNoiseGenerator();
            marchingCubesGenerator = GetMarchingCubesGenerator();
        }
    }

    void Update()
    {
        if (Application.isPlaying || updateInEditor)
        {
            if (DidPlayerMoved(viewer))
            {
                if (noiseGenerator.HasNoises())
                {
                    noiseGenerator.ClearOldNoises();
                    marchingCubesGenerator.ClearOldMC();
                }
                GetPlayerDirections();
                Pooling();
            }

            if (noiseGenerator.HasAComputedNoise())
                StartMCComputeFromNoise();
            if (marchingCubesGenerator.HasAComputedMC())
                UpdateMeshFromMC();

            //foreach (var chunk in chunks.Values)
            //{
            //    if (chunk.GetCurrentMesh() != null)
            //    {          
            //        Matrix4x4 matrix4X4 = Matrix4x4.TRS(chunk.GetPos().world, Quaternion.identity, Vector3.one);
            //        Graphics.RenderMesh(renderParams, chunk.GetCurrentMesh(), 0, matrix4X4);
            //    }
            //}
        }
    }

    public void Generate()
    {
        noiseGenerator = GetComponent<NoiseGenerator>();
        marchingCubesGenerator = GetComponent<MarchingCubesGenerator>();

        Init();
        GenerateSpawn();

        renderParams = new RenderParams(meshMaterial);
    }

    public NoiseGenerator GetNoiseGenerator()
    {
        if (!noiseGenerator)
            noiseGenerator = GetComponent<NoiseGenerator>();

        return noiseGenerator;
    }

    public MarchingCubesGenerator GetMarchingCubesGenerator()
    {
        if (!marchingCubesGenerator)
            marchingCubesGenerator = GetComponent<MarchingCubesGenerator>();

        return marchingCubesGenerator;
    }

    void Init()
    {
        chunks = new Dictionary<MapParameters.Positions, Chunk>();
        currChunkPos = new MapParameters.Positions(Vector3Int.zero, Vector3.zero);
        currDirections = new List<MapParameters.Directions>();

        int nbGOPerAxis = viewer.GetViewDistance() * 2 + 1;
        pooledGameObjects = new PooledGameObject[nbGOPerAxis * nbGOPerAxis * nbGOPerAxis];

        unusedGO = new Queue<PooledGameObject>();

        marchingCubesGenerator.SetVertexColorParam(vertexColorParameters);
    }

    void GenerateSpawn()
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
                    currChunkPos.Set(currChunkGridPos);

                    GameObject go = new GameObject("GO_pooled_" + goCount);
                    MeshFilter filter = go.AddComponent<MeshFilter>();
                    MeshCollider collider = go.AddComponent<MeshCollider>();
                    MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                    renderer.material = meshMaterial;

                    PooledGameObject pooledGameObject = new PooledGameObject(go, currChunkPos, filter, collider, renderer, transform);
                    pooledGameObjects[goCount++] = pooledGameObject;
                    AddChunk(pooledGameObject, currChunkPos, resolution);
                }
            }
        }
    }

    bool DidPlayerMoved(Viewer _viewer)
    {
        Vector3Int chunkGridPos = WorldToGridPos(viewer.GetCurrentWorldPosition());
        viewer.SetCurrentGridPos(chunkGridPos);

        return viewer.GetCurrentGridPosition() != viewer.GetLastGridPosition();
    }

    void Pooling()
    {
        for (int i = 0; i < currDirections.Count; i++)
        {
            MapParameters.Directions currDirection = currDirections[i];
            MapParameters.Directions oldDirection = ((int)currDirections[i] % 2 == 0) ? currDirections[i] + 1 : currDirections[i] - 1;

            Clean(oldDirection);
            ReuseAndSetChunk(currDirection);
        }

        currDirections.Clear();
        viewer.SetLastGridPos(viewer.GetCurrentGridPosition());
    }

    void StartMCComputeFromNoise()
    {
        NoiseGenerator.Noise[] noises = noiseGenerator.DequeueNoisesComputed();

        foreach (NoiseGenerator.Noise noise in noises)
        {
            Vector3Int noisePos = noise.GetPos().grid;
            NativeArray<float> densityValues = noise.GetComputeAsync();
            MarchingCubesGenerator.MarchingCubes marchingCubes = marchingCubesGenerator.CreateMCInstance(densityValues, resolution, noisePos.x, noisePos.y, noisePos.z);
            marchingCubes.StartComputeAsync();
            noise.DisposeData();
        }
    }

    void UpdateMeshFromMC()
    {
        MarchingCubesGenerator.MarchingCubes[] marchingCubesComputeds = marchingCubesGenerator.DequeueMCsComputed();

        foreach (MarchingCubesGenerator.MarchingCubes mc in marchingCubesComputeds)
        {
            Chunk chunk = chunks[mc.GetPos()];
            Mesh mesh = mc.GetComputeAsync();
            mc.DisposeData();

            chunk.SetPos(mc.GetPos());
            chunk.SetCachedMesh(mesh);
            PooledGameObject pooledGameObject = chunk.GetCurrentPooledGameObject();
            pooledGameObject.UpdateCurrentMesh(mesh);
        }
    }

    void Clean(MapParameters.Directions _direction)
    {
        Vector3Int lastGridPos = viewer.GetLastGridPosition();

        foreach (Vector3Int offset in viewer.GetOffsetsDirection(_direction))
        {
            currChunkGridPos.Set(lastGridPos.x + offset.x,
                                 lastGridPos.y + offset.y,
                                 lastGridPos.z + offset.z);

            currChunkPos.Set(currChunkGridPos);

            Chunk chunk = chunks[currChunkPos];
            PooledGameObject pooledGameObject = chunk.GetCurrentPooledGameObject();
            pooledGameObject.SetUsed(false);
            unusedGO.Enqueue(pooledGameObject);
        }
    }

    void ReuseAndSetChunk(MapParameters.Directions _direction)
    {
        Vector3Int currGridPos = viewer.GetCurrentGridPosition();

        foreach (Vector3Int offset in viewer.GetOffsetsDirection(_direction))
        {
            currChunkGridPos.Set(currGridPos.x + offset.x,
                                 currGridPos.y + offset.y,
                                 currGridPos.z + offset.z);

            currChunkPos.Set(currChunkGridPos);

            PooledGameObject pooledGameObject = unusedGO.Dequeue();

            pooledGameObject.SetUsed(true);
            pooledGameObject.SetPositions(currChunkPos);
            pooledGameObject.RemoveMesh();

            Chunk chunk;
            bool hasChunk = chunks.TryGetValue(currChunkPos, out chunk);

            if (hasChunk && chunk.HasCachedMesh())
            {
                pooledGameObject.UpdateCurrentMesh(chunk.GetCachedMesh());
            }
            else
            {
                AddChunk(pooledGameObject, currChunkPos, resolution);
            }
        }
    }

    void GetPlayerDirections()
    {
        Vector3Int currMovement = viewer.GetCurrentGridPosition() - viewer.GetLastGridPosition();

        for (int i = 0; i < 3; i++)
        {
            MapParameters.Directions currDirection = i == 0 ? MapParameters.Directions.Right :
                                                     i == 1 ? MapParameters.Directions.Up :
                                                     MapParameters.Directions.Forward;

            if (currMovement[i] != 0)
            {
                currDirections.Add(currMovement[i] < 0 ? currDirection + 1 : currDirection);
            }
        }
    }

    Chunk AddChunk(PooledGameObject _go, MapParameters.Positions _chunkPos, int _lod)
    {
        Chunk chunk = null;

        if (chunks.ContainsKey(_chunkPos))
            chunk = chunks[_chunkPos];
        else
        {
            chunk = new Chunk();
            chunks.Add(_chunkPos, chunk);
        }

        chunk.SetCurrentPooledGameObject(_go);

        NoiseGenerator.Noise noiseInstance = noiseGenerator.CreateNoiseInstance(_lod, _chunkPos.grid.x, _chunkPos.grid.y, _chunkPos.grid.z);
        noiseInstance.StartComputeAsync();

        return chunk;
    }

    Vector3Int WorldToGridPos(Vector3 _worldPos)
    {
        return new Vector3Int(
           (int)(_worldPos.x / MapParameters.GetChunkAxisSize()),
           (int)(_worldPos.y / MapParameters.GetChunkAxisSize()),
           (int)(_worldPos.z / MapParameters.GetChunkAxisSize()));
    }

    void OnApplicationQuitting()
    {
        noiseGenerator.ClearOldNoises();
        marchingCubesGenerator.ClearOldMC();
    }
}

