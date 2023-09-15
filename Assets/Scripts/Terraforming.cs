using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Terraforming : MonoBehaviour
{
    [SerializeField] float radiusTerraforming = 2f;
    [SerializeField] float strength = 1f;
    [SerializeField] float speed = 1f;

    Camera cam;
    RaycastHit[] hits;
    Chunk chunkHitted;
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
            var hitColliders = Physics.OverlapSphere(hits[0].point, radiusTerraforming + 5);

            chunkHitted = null;
            foreach (Collider col in hitColliders)
            {
                MapGenerator.Instance.GetChunkWithWorldPos(col.transform.position, out chunkHitted);
                chunkHitted?.Edit(hits[0].point, radiusTerraforming, _isConstruct, strength * Time.deltaTime * speed);
            }
        }
    }
}
