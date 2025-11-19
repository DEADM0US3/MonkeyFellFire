using UnityEngine;

public class SplitSubmeshes : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (!mf || !mr)
        {
            Debug.LogError("No hay MeshFilter o MeshRenderer en este objeto.");
            return;
        }

        Mesh mesh = mf.sharedMesh;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            GameObject part = new GameObject("Submesh_" + i);
            part.transform.parent = transform;
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;

            MeshRenderer newMR = part.AddComponent<MeshRenderer>();
            newMR.material = mr.sharedMaterials[i];

            MeshFilter newMF = part.AddComponent<MeshFilter>();

            Mesh newMesh = new Mesh();
            newMesh.vertices = mesh.vertices;
            newMesh.normals = mesh.normals;
            newMesh.uv = mesh.uv;
            newMesh.triangles = mesh.GetTriangles(i);

            newMF.mesh = newMesh;

            part.AddComponent<MeshCollider>();
        }

        Debug.Log("Submeshes separados correctamente.");
    }
}
