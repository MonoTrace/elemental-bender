using UnityEngine;
public class ElementalMaterialCleanup : MonoBehaviour {
    public Material material;
    void OnDestroy() { if(material) Destroy(material); }
}
