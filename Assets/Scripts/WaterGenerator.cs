// Water generation disabled: moving water generation out of this module.
// For now we keep a stub so other scripts that reference WaterGenerator
// compile, but it does nothing to avoid creating water meshes that
// currently cause collision/visual issues.
using UnityEngine;

public class WaterGenerator : MonoBehaviour
{
    // Stub: no water generation for now.
    public void AddWater() { /* intentionally empty */ }
}
