// In Editor Assemblie
// You need the CoexistanceManager.cs script for its RenderPipelineSavedOrder and the LightmapData.cs script
// See LightmapData.cs for usage
using UnityEditor;
using UnityEngine;

namespace Coexistance
{
    [CustomEditor(typeof(LightmapData))]
    class LightmapDataEditor : Editor
    {
        SerializedProperty m_Lightings;
        GUIContent[] m_Labels;

        void OnEnable()
        {
            m_Lightings = serializedObject.FindProperty("m_Lightings");

        }

        public override void OnInspectorGUI()
        {
            var labels = RenderPipelineSavedOrder.GetLabels();

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            int length = RenderPipelineSavedOrder.length;
            if (m_Lightings.arraySize != length)
            {
                m_Lightings.arraySize = length;
                GUI.changed = true;
            }

            for (int i = 0; i < length; ++i)
                EditorGUILayout.ObjectField(m_Lightings.GetArrayElementAtIndex(i), labels[i]);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}