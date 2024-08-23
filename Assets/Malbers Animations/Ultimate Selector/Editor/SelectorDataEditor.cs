using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEditor;
using UnityEditor.Events;
using MalbersAnimations.Events;

namespace MalbersAnimations.Selector
{
    [CustomEditor(typeof(SelectorData))]
    public class SelectorDataEditor : Editor
    {
        SelectorData M;
        private void OnEnable()
        {
            M = (SelectorData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical(MalbersEditor.StyleBlue);
            EditorGUILayout.HelpBox("Data values to save on each Selector", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical(MalbersEditor.StyleGray);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            DrawDefaultInspector();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(M);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}