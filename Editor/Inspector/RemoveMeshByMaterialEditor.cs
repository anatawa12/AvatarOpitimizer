using UnityEditor;

namespace Anatawa12.AvatarOptimizer;

[CustomEditor(typeof(RemoveMeshByMaterial))]
public class RemoveMeshByMaterialEditor : AvatarTagComponentEditorBase
{
    // TODO
    protected override void OnInspectorGUIInner()
    {
        DrawDefaultInspector();
    }
}
