using System;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    static MapGenerator instance;
    public static MapGenerator Instance { get => instance; }

    [Header("Map Parameters")]
    [SerializeField, Range(1, 5)] int LOD = 1;
    [SerializeField, Min(2), Tooltip("Multiple of 8")] int nbPointsPerChunk = 8;
    [SerializeField] Material material;

    [Serializable]
    class Viewer
    {
        public GameObject go;
        public int viewDistance;
        [HideInInspector] public Vector2Int[] lastChunksPos;
        [HideInInspector] public Vector2Int viewerPosGrid;
        [HideInInspector] public Vector2Int lastViewerPosGrid;
    }

    [Header("Viewers Parameters")]
    [SerializeField] Viewer[] viewers;

    Dictionary<Vector2Int, Chunk> chunks;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        chunks = new Dictionary<Vector2Int, Chunk>();

        foreach (Viewer viewer in viewers)
        {
            int sizeChunksLimit = (2 * viewer.viewDistance + 1) * (2 * viewer.viewDistance + 1);
            viewer.lastChunksPos = new Vector2Int[sizeChunksLimit];

            Vector3 viewerPos = viewer.go.transform.position;
            viewer.viewerPosGrid = new Vector2Int
                       (Mathf.RoundToInt(viewerPos.x / nbPointsPerChunk),
                        Mathf.RoundToInt(viewerPos.z / nbPointsPerChunk));

            viewer.lastViewerPosGrid = Vector2Int.zero;
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
            Vector3 viewerPos = viewer.go.transform.position;
            viewer.viewerPosGrid = new Vector2Int
                       (Mathf.RoundToInt(viewerPos.x / nbPointsPerChunk),
                        Mathf.RoundToInt(viewerPos.z / nbPointsPerChunk));

            if (viewer.viewerPosGrid != viewer.lastViewerPosGrid || chunks.Count == 0)
            {
                ActiveVisibleChunk();
                viewer.lastViewerPosGrid = viewer.viewerPosGrid;
            }
        }
    }

    Chunk CreateNewChunk(Vector2Int _chunkGridPos)
    {
        Chunk newChunk = new Chunk("Chunk " + chunks.Count, material);
        newChunk.SetParent(transform);
        newChunk.CreateChunk(nbPointsPerChunk, _chunkGridPos.x, 0, _chunkGridPos.y);
        chunks.Add(new Vector2Int(_chunkGridPos.x, _chunkGridPos.y), newChunk);
        return newChunk;
    }

    public void ActiveVisibleChunk()
    {
        for (int i = 0; i < viewers.Length; i++)
        {
            int viewDistance = viewers[i].viewDistance;

            int neighbourIndex = 0;
            bool hasAlreadyLoadChunk = chunks.Count > 0;

            for (int z = -viewDistance; z < viewDistance + 1; z++)
            {
                for (int x = -viewDistance; x < viewDistance + 1; x++)
                {
                    Vector2Int nextNeighbourPos = new Vector2Int(x + viewers[i].viewerPosGrid.x, z + viewers[i].viewerPosGrid.y);

                    Chunk chunkNeighbour = null;
                    if (!chunks.TryGetValue(nextNeighbourPos, out chunkNeighbour))
                    {
                        chunkNeighbour = CreateNewChunk(nextNeighbourPos);
                    }

                    chunkNeighbour.SetActive(true);

                    if (hasAlreadyLoadChunk && IsThisChunkTooFar(viewers[i], neighbourIndex))
                    {
                        Vector2Int lastChunkPos = viewers[i].lastChunksPos[neighbourIndex];
                        chunks[lastChunkPos].SetActive(false);
                    }

                    viewers[i].lastChunksPos[neighbourIndex] = nextNeighbourPos;
                    neighbourIndex++;
                }
            }
        }
    }

    bool IsThisChunkTooFar(Viewer _viewer, int _neighbourIndex)
    {
        return (Mathf.Abs(_viewer.viewerPosGrid.x - _viewer.lastChunksPos[_neighbourIndex].x) > _viewer.viewDistance ||
        Mathf.Abs(_viewer.viewerPosGrid.y - _viewer.lastChunksPos[_neighbourIndex].y) > _viewer.viewDistance);
    }
}
