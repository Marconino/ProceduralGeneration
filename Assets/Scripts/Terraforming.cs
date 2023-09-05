using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Terraforming : MonoBehaviour
{
    [SerializeField] float radiusTerraforming = 2f;

    Camera cam;
    RaycastHit[] hits;
    Chunk currChunk;

    private void Start()
    {
        cam = GetComponent<Camera>();
        hits = new RaycastHit[1];
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButton(0))
        {
            Terraform(true);
        }
        else if (Input.GetMouseButton(1))
        {
            Terraform(false);
        }

    }

    void Terraform(bool _isConstruct)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.RaycastNonAlloc(ray, hits) == 1)
        {
            Chunk chunkHitted = null;
            MapGenerator.Instance.GetChunkWithWorldPos(hits[0].point, out chunkHitted);
            currChunk = chunkHitted;

            if (chunkHitted != null)
            {
                Debug.Log("Je suis aux positions : " + hits[0].point + " et au chunk aux positions : " + chunkHitted.GetPos());
                Vector3 hitPos = new Vector3(hits[0].point.x, 0, hits[0].point.z);
                chunkHitted.Edit(hits[0].point, radiusTerraforming, _isConstruct);
            }

        }
    }

    private void OnDrawGizmos()
    {
        if (hits != null && hits.Length > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hits[0].point, radiusTerraforming);
            Debug.DrawLine(transform.position, hits[0].point, Color.white, 3f);
        }


        if (currChunk != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireMesh(currChunk.GetMesh(), currChunk.GetPos());

        }
    }
}
