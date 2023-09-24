using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapParameters;

public class Viewer : MonoBehaviour
{
    [SerializeField] int viewDistance;
    Vector3Int gridPos;
    Vector3Int lastGridPos;
    MapParameters.DirectionsOffset[] offsetDirections;

    private void Start()
    {
        lastGridPos = gridPos = Vector3Int.zero;
        offsetDirections = new MapParameters.DirectionsOffset[6];

        CreateDirectionsOffset();
    }

    void CreateDirectionsOffset()
    {
        for (int currDirection = 0; currDirection < (int)Directions.Count; currDirection++)
        {
            offsetDirections[currDirection].offsets = new Vector3Int[(viewDistance * 2 + 1) * (viewDistance * 2 + 1)];
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
                        offsetDirections[currDirection].offsets[index] = new Vector3Int(valueNotChanged, i, j);
                    }
                    else if (currDirection <= (int)Directions.Down)
                    {
                        offsetDirections[currDirection].offsets[index] = new Vector3Int(-j, valueNotChanged, -i);
                    }
                    else
                    {
                        offsetDirections[currDirection].offsets[index] = new Vector3Int(j, i, valueNotChanged);
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
    public Vector3Int[] GetOffsetDirection(Directions _direction)
    {
        return offsetDirections[(int)_direction].offsets;
    }
}
