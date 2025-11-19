using UnityEngine;
using System.Linq;

public class DisableColliderOnTransparent : MonoBehaviour
{
    void Start()
    {
        // 1. Obtiene todos los renderers del FBX
        var renderers = GetComponentsInChildren<MeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            bool isTransparent = renderer.sharedMaterials
                .Any(m => m != null && m.renderQueue >= 3000);

            if (!isTransparent) continue;

            // 2. Si es un objeto transparente, obtiene su MESH
            var mf = renderer.GetComponent<MeshFilter>();
            if (mf != null) continue;

            Mesh transparentMesh = mf.sharedMesh;

            // 3. Encuentra TODOS los MeshCollider del FBX
            var colliders = GetComponentsInChildren<MeshCollider>(true);

            foreach (var col in colliders)
            {
                // 4. Si el mesh del collider coincide → BORRAR
                if (col.sharedMesh == transparentMesh)
                {
                    Debug.Log("Quitando collider de: " + renderer.name);
                    Destroy(col);
                }
            }
        }
    }
}
