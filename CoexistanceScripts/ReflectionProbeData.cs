// In Runtime Assemblie
// Put this script, on same object that have Reflection Probe in URP:
//  - in case this scene is also opened in HDRP, the custom cubemap may be lost This will help retrieve it
using UnityEngine;

namespace Coexistance
{
    [DisallowMultipleComponent]
    public class ReflectionProbeData : MonoBehaviour
    {
    #if UNITY_EDITOR
        [SerializeField] Cubemap m_CustomCubemap;

        public Cubemap customCubemap => m_CustomCubemap;
    #endif
    }
}