using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Anatawa12.AvatarOptimizer
{
    [CustomEditor(typeof(MergeSkinnedMesh))]
    internal class MergeSkinnedMeshEditor : AvatarTagComponentEditorBase
    {
        private static class Style
        {
            public static readonly GUIStyle ErrorStyle = new GUIStyle
            {
                normal = { textColor = Color.red },
                wordWrap = false,
            };

            public static readonly GUIStyle WarningStyle = new GUIStyle
            {
                normal = { textColor = Color.yellow },
                wordWrap = false,
            };
        }

        SerializedProperty _renderersSetProp = null!; // initialized in OnEnable
        SerializedProperty _staticRenderersSetProp = null!; // initialized in OnEnable
        SerializedProperty _removeEmptyRendererObjectProp = null!; // initialized in OnEnable
        SerializedProperty _skipEnablementMismatchedRenderers = null!; // initialized in OnEnable
        PrefabSafeSet.EditorUtil<Material> _doNotMergeMaterials = null!; // initialized in OnEnable

        private void OnEnable()
        {
            _renderersSetProp = serializedObject.FindProperty("renderersSet");
            _staticRenderersSetProp = serializedObject.FindProperty("staticRenderersSet");
            _removeEmptyRendererObjectProp = serializedObject.FindProperty("removeEmptyRendererObject");
            _skipEnablementMismatchedRenderers =
                serializedObject.FindProperty(nameof(MergeSkinnedMesh.skipEnablementMismatchedRenderers));
            var nestCount = PrefabSafeSet.PrefabSafeSetUtil.PrefabNestCount(serializedObject.targetObject);
            _doNotMergeMaterials = PrefabSafeSet.EditorUtil<Material>.Create(
                serializedObject.FindProperty("doNotMergeMaterials"),
                nestCount,
                x => (Material)x.objectReferenceValue,
                (x, v) => x.objectReferenceValue = v);
        }

        protected override void OnInspectorGUIInner()
        {
            var component = (MergeSkinnedMesh)target;
            if (component.GetComponent<SkinnedMeshRenderer>().sharedMesh)
            {
                EditorGUILayout.HelpBox(AAOL10N.Tr("MergeSkinnedMesh:warning:MeshIsNotNone"), MessageType.Warning);
            }

            EditorGUILayout.PropertyField(_renderersSetProp);
            EditorGUILayout.PropertyField(_staticRenderersSetProp);
            EditorGUILayout.PropertyField(_removeEmptyRendererObjectProp);
            EditorGUILayout.PropertyField(_skipEnablementMismatchedRenderers);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.LabelField(AAOL10N.Tr("MergeSkinnedMesh:label:Merge Materials"), EditorStyles.boldLabel);
            if (targets.Length != 1)
                EditorGUILayout.LabelField("MergeMaterial is not supported with Multi Target Editor");
            else
                MergeMaterials((MergeSkinnedMesh)target);
        }

        public void MergeMaterials(MergeSkinnedMesh merge)
        {
            var materials = new HashSet<Material?>();
            var renderersSetAsList = merge.renderersSet.GetAsList();
            var staticRenderersSetAsList = merge.staticRenderersSet.GetAsList();
            var ofRenderers = renderersSetAsList.Select(EditSkinnedMeshComponentUtil.GetMaterials);
            var ofStatics = staticRenderersSetAsList.Select(x => x.sharedMaterials);
            foreach (var group in ofRenderers.Concat(ofStatics)
                         .SelectMany((x, renderer) => x.Select((mat, material) => (mat, renderer, material)))
                         .GroupBy(x => x.mat))
            {
                if (group.Key == null) continue;
                materials.Add(group.Key);
                if (group.Count() == 1)
                {
                    continue;
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(group.Key, typeof(Material), true);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                var element = _doNotMergeMaterials.GetElementOf(group.Key);
                var fieldPosition = EditorGUILayout.GetControlRect();
                var label = new GUIContent(AAOL10N.Tr("MergeSkinnedMesh:label:Merge"));
                using (new PrefabSafeSet.PropertyScope<Material>(element, fieldPosition, label))
                    element.SetExistence(!EditorGUI.ToggleLeft(fieldPosition, label, !element.Contains));

                EditorGUILayout.LabelField(AAOL10N.Tr("MergeSkinnedMesh:label:Renderers"));
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                foreach (var (_, rendererIndex, _) in group)
                {
                    if (rendererIndex < renderersSetAsList.Count)
                        EditorGUILayout.ObjectField(renderersSetAsList[rendererIndex], typeof(SkinnedMeshRenderer),
                            true);
                    else
                        EditorGUILayout.ObjectField(staticRenderersSetAsList[rendererIndex - renderersSetAsList.Count],
                            typeof(MeshRenderer), true);
                }

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
