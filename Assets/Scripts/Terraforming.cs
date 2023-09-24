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
    Chunk[] chunksHitted;

    Vector3[] offsetsCube;
    Vector3[] cornersPosCube;
    Bounds bounds;
    private void Start()
    {
        cam = GetComponent<Camera>();
        hits = new RaycastHit[1];

        float halfRadius = radiusTerraforming / 2f;

        offsetsCube = new Vector3[8]
        {
            new Vector3(-halfRadius, -halfRadius, -halfRadius), new Vector3(-halfRadius, -halfRadius, halfRadius), new Vector3(halfRadius,-halfRadius, halfRadius), new Vector3(halfRadius, -halfRadius, -halfRadius),
            new Vector3(-halfRadius, halfRadius,-halfRadius), new Vector3(-halfRadius, halfRadius, halfRadius), new Vector3(halfRadius, halfRadius, halfRadius), new Vector3(halfRadius, halfRadius, -halfRadius)
        };
        cornersPosCube = new Vector3[8];

        bounds = new Bounds(Vector3.zero, new Vector3(radiusTerraforming+1, radiusTerraforming+1, radiusTerraforming+1));
    }
    private void Update()
    {
        transform.position = new Vector3(transform.position.x + 1f, transform.position.y, transform.position.z);
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
            //for (int i = 0; i < offsetsCube.Length; i++)
            //{
            //    //newPos[i] += new Vector3(40, -42.5f, 30);
            //    cornersPosCube[i] = hits[0].point + offsetsCube[i];
            //}
            //MapGenerator.Instance.GetChunksWithWorldPos(cornersPosCube, out chunksHitted);
            //foreach (Chunk chunk in chunksHitted)
            //{
            //    chunk.Edit(new Vector3(hits[0].point.x,
            //        hits[0].point.y,
            //        hits[0].point.z), radiusTerraforming, _isConstruct, strength * Time.deltaTime * speed);
            //}

            bounds.center = hits[0].point;
            MapGenerator.Instance.GetChunksWithWorldPos(bounds, out chunksHitted);

            foreach(var chunk in chunksHitted)
            {
                chunk.Edit(new Vector3(hits[0].point.x, hits[0].point.y, hits[0].point.z),
                    radiusTerraforming, _isConstruct, strength * Time.deltaTime * speed);
            }

            //Collider[] var = Physics.OverlapSphere(new Vector3(40, -42.5f, 30), radiusTerraforming);

            //foreach (Collider col in hitColliders)
            //{
            //    //hits[0].point = new Vector3(hits[0].point.x + MapParameters.ChunkSize / 2 f,
            //    //    hits[0].point.y + MapParameters.²1ChunkSize / 2f,
            //    //    hits[0].point.z + MapParameters.ChunkSize / 2f);

            //    MapGenerator.Instance.GetChunkWithWorldPos(col.transform.position, out chunkHitted);

            //    Debug.Log(col.gameObject.name + " Positions : " + hits[0].point);
            //    chunkHitted?.Edit(new Vector3(hits[0].point.x ,
            //        hits[0].point.y,
            //        hits[0].point.z), radiusTerraforming, _isConstruct, strength * Time.deltaTime * speed);
            //}
        }
    }

    private void OnDrawGizmos()
    {
        //if (hits[0].collider != null)
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawWireSphere(hits[0].point, radiusTerraforming);
        //    float halfSideLength = radiusTerraforming / 2f;

        //    Gizmos.color = Color.black;
        //    // Déplacer les sommets en fonction du centre A
            
        //     Gizmos.DrawWireCube(bounds.center, bounds.size);

        //    //Gizmos.color = Color.cyan;
        //    //Gizmos.DrawLine(hits[0].point -
        //    //    new Vector3(radiusTerraforming / 2f, radiusTerraforming / 2f, radiusTerraforming / 2f)
        //    //    , hits[0].point +
        //    //    new Vector3(radiusTerraforming / 2f, radiusTerraforming / 2f, radiusTerraforming / 2f));

        //    //Gizmos.color = Color.cyan;
        //    //Gizmos.DrawLine(hits[0].point -
        //    //    new Vector3(radiusTerraforming / 2f, radiusTerraforming / 2f, -radiusTerraforming / 2f)
        //    //    , hits[0].point +
        //    //    new Vector3(radiusTerraforming / 2f, radiusTerraforming / 2f, -radiusTerraforming / 2f));


        //    //Gizmos.color = Color.white;
        //    //Gizmos.DrawLine(transform.position, hits[0].point);
        //}

    }
}
