// In Runtime Assemblie
// Put this script, one per scene. You need the CoexistanceManager.cs script for its RenderPipelineSavedOrder and the LightmapDataEditor.cs script:
//  - Lightmap baked are different per pipeline, this will store each and help to automatically swap them.
//  - One entry will appears in the inspector per SRP in the project.
using UnityEditor;
using UnityEngine;

namespace Coexistance
{
    [DisallowMultipleComponent]
    public class LightmapData : MonoBehaviour
    {
    #if UNITY_EDITOR
        [SerializeField] LightingDataAsset[] m_Lightings = new LightingDataAsset[0];

        public LightingDataAsset this[int i]
            => (i < 0 || i >= m_Lightings.Length) ? null : m_Lightings[i];

        public int length => m_Lightings.Length;
    #endif
    }
}