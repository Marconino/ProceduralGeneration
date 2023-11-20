using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapParameters;

public class Viewer : MonoBehaviour
{
    [SerializeField] int viewDistance;
    Vector3Int gridPos;
    Vector3Int lastGridPos;
    Vector3Int[][] offsetsDirections;

    private void Start()
    {
        lastGridPos = gridPos = Vector3Int.zero;
        offsetsDirections = new Vector3Int[(int)Directions.Count][];

        CreateDirectionsOffset();
    }

    void CreateDirectionsOffset()
    {
        for (int currDirection = 0; currDirection < (int)Directions.Count; currDirection++)
        {
            offsetsDirections[currDirection] = new Vector3Int[(viewDistance * 2 + 1) * (viewDistance * 2 + 1)];
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
                        offsetsDirections[currDirection][index] = new Vector3Int(valueNotChanged, i, j);
                    }
                    else if (currDirection <= (int)Directions.Down)
                    {
                        offsetsDirections[currDirection][index] = new Vector3Int(-j, valueNotChanged, -i);
                    }
                    else
                    {
                        offsetsDirections[currDirection][index] = new Vector3Int(j, i, valueNotChanged);
                    }
                    index++;
                }
            }
        }
    }

    public int GetViewDistance()
    {
        return viewDistance;
    }
    public Vector3Int GetCurrentGridPosition()
    {
        return gridPos;
    }   
    public Vector3Int GetLastGridPosition()
    {
        return lastGridPos;
    }
    public Vector3 GetCurrentWorldPosition()
    {
        return transform.position;
    }
    public void SetCurrentGridPos(Vector3Int _gridPos)
    {
        gridPos = _gridPos;
    }
    public void SetLastGridPos(Vector3Int _lastGridPos)
    {
        lastGridPos = _lastGridPos;
    }
    public Vector3Int[] GetOffsetsDirection(Directions _direction)
    {
        return offsetsDirections[(int)_direction];
    }
}
