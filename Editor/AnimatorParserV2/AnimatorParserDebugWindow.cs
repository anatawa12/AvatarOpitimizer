using System;
using System.Linq;
using System.Text;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anatawa12.AvatarOptimizer.AnimatorParsersV2
{
    internal class AnimatorParserDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Avatar Optimizer/Animator Parser Debug Window V2")]
        private static void Open() => GetWindow<AnimatorParserDebugWindow>("AnimatorParser Debug Window V2");

        public ParserSource parserSource;
        public RuntimeAnimatorController? animatorController;
        public GameObject? avatar;
        public GameObject? rootGameObject;
        public Motion? motion;

        public Vector2 scrollView;

        public GameObject? parsedRootObject;
        public INodeContainer? Container;

        private void OnGUI()
        {
            OnParserSourceGUI();

            using (new EditorGUI.DisabledScope(!parsedRootObject))
            {
                if (GUILayout.Button("Copy Parsed Text"))
                    GUIUtility.systemCopyBuffer = CreateText();
                if (GUILayout.Button("Copy Detailed Parsed Text"))
                    GUIUtility.systemCopyBuffer = CreateText(true);
            }

            if (Container == null) return;

            scrollView = GUILayout.BeginScrollView(scrollView);

            foreach (var group in Container.FloatNodes.GroupBy(x => x.Key.target))
            {
                EditorGUILayout.ObjectField(group.Key, typeof(Object), true);
                EditorGUI.indentLevel++;
                foreach (var ((_, propName), propState) in group)
                {
                    string propStateInfo = "";

                    if (!propState.AppliedAlways)
                        propStateInfo += "Partial:";

                    if (propState.Value.PossibleValues is float[] values)
                        propStateInfo += $"Const:{string.Join(",", values)}";
                    else
                        propStateInfo += "Variable";

                    NarrowValueLabelField(propName, propStateInfo);
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.EndScrollView();
        }

        private string CreateText(bool detailed = false)
        {
            var root = parsedRootObject!.transform;
            var resultText = new StringBuilder();
            
            foreach (var group in Container!.FloatNodes.GroupBy(x => x.Key.target))
            {
                var gameObject = group.Key.transform;
                resultText.Append(Utils.RelativePath(root, gameObject)).Append(": ")
                    .Append(((Object)group.Key).GetType().FullName).Append('\n');

                foreach (var ((_, propName), propState) in group)
                {
                    string propStateInfo = "";

                    if (!propState.AppliedAlways)
                        propStateInfo += "Partial:";

                    if (propState.Value.PossibleValues is float[] values)
                        propStateInfo += $"Const:{string.Join(",", values)}";
                    else
                        propStateInfo += "Variable";

                    resultText.Append("  ").Append(propName).Append(": ").Append(propStateInfo).Append('\n');
                    if (detailed)
                        AppendNodeRecursive(propState, resultText, "    ");
                }

                resultText.Append('\n');
            }

            return resultText.ToString();
        }

        private void AppendNodeRecursive(PropModNode<float> propState, StringBuilder resultText, string indent)
        {
            switch (propState)
            {
                case AnimatorControllerPropModNode<float> animCont:
                    resultText.Append($"{indent}AnimatorController: \n");
                    foreach (var layerInfo in animCont.LayersReversed)
                    {
                        resultText.Append($"{indent}  Layer {layerInfo.LayerIndex}: {layerInfo.Weight}, {layerInfo.BlendingMode}\n");
                        AppendNodeRecursive(layerInfo.Node, resultText, indent + "    ");
                    }
                    break;
                case AnimationComponentPropModNode<float> animation:
                    resultText.Append($"{indent}Animation: {animation.Component.name}\n");
                    AppendNodeRecursive(animation.Animation, resultText, indent + "  ");
                    break;
                case AnimatorPropModNode<float> animator:
                    resultText.Append($"{indent}Animator: {animator.Component.name}\n");
                    foreach (var layerInfo in animator.LayersReversed)
                    {
                        resultText.Append($"{indent}  Layer {layerInfo.LayerIndex}: {layerInfo.Weight}, {layerInfo.BlendingMode}\n");
                        AppendNodeRecursive(layerInfo.Node, resultText, indent + "    ");
                    }
                    break;
                case HumanoidAnimatorPropModNode humanoid:
                    resultText.Append($"{indent}Humanoid: {humanoid.Component.name}\n");
                    break;
                case VariableComponentPropModNode<float> variable:
                    resultText.Append($"{indent}Variable({variable.Component.GetType().Name}): {variable.Component.name}\n");
                    break;
                case AnimatorLayerPropModNode<float> animatorLayer:
                    resultText.Append($"{indent}AnimatorLayer:\n");
                    foreach (var childNode in animatorLayer.Children)
                        AppendNodeRecursive(childNode, resultText, indent + "  ");
                    break;
                case AnimatorStatePropModNode<float> stateNode:
                    resultText.Append($"{indent}AnimatorState: {stateNode.State.name}\n");
                    AppendNodeRecursive(stateNode.Node, resultText, indent + "  ");
                    break;
                case BlendTreeNode<float> blendTreeNode:
                    resultText.Append($"{indent}BlendTree:\n");
                    foreach (var childNode in blendTreeNode.Children)
                    {
                        resultText.Append($"{indent}  BlendTreeElement({childNode.Index}):\n");
                        AppendNodeRecursive(childNode.Node, resultText, indent + "    ");
                    }
                    break;
                case FloatAnimationCurveNode curve:
                    resultText.Append($"{indent}AnimationCurve: {curve.Clip.name}\n");
                    break;
                case RootPropModNode<float> rootNode:
                    resultText.Append($"{indent}Root:\n");
                    foreach (var rootNodeChild in rootNode.Children)
                    {
                        resultText.Append($"{indent}  {rootNodeChild.Component.name}:\n");
                        AppendNodeRecursive(rootNodeChild.Node, resultText, indent + "    ");
                    }
                    break;
                default:
                    resultText.Append($"{indent}Unknown: {propState.GetType().Name}\n");
                    break;
            }
        }


        private static void NarrowValueLabelField(string label0, string value)
        {
            var position = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(true, 18f));

            var valueWidth = EditorGUIUtility.labelWidth;
            var labelPosition = new Rect(position.x, position.y, position.width - valueWidth, position.height);
            var valuePosition = new Rect(position.x + labelPosition.width + 2, position.y, valueWidth - 2, position.height);

            GUI.Label(labelPosition, label0);
            GUI.Label(valuePosition, value);
        }

        void OnParserSourceGUI()
        {
            parserSource = (ParserSource)EditorGUILayout.EnumPopup(parserSource);
            switch (parserSource)
            {
                case ParserSource.WholeAvatar:
                    ObjectField(ref avatar, "Avatar", true);

                    using (new EditorGUI.DisabledScope(!avatar))
                    {
                        if (GUILayout.Button("Parse") && avatar)
                        {
                            parsedRootObject = avatar;
                            Container = new AnimatorParser(true).GatherAnimationModifications(
                                new BuildContext(avatar, null));
                        }
                    }

                    break;
                case ParserSource.AnimatorController:
                    ObjectField(ref animatorController, "Controller", false);
                    ObjectField(ref rootGameObject, "Root GameObject", true);

                    using (new EditorGUI.DisabledScope(!animatorController || !rootGameObject))
                    {
                        if (GUILayout.Button("Parse") && animatorController != null && rootGameObject != null)
                        {
                            parsedRootObject = rootGameObject;
                            Container = new AnimatorParser(true)
                                .ParseAnimatorController(rootGameObject, animatorController);
                        }
                    }

                    break;
                case ParserSource.Motion:
                    ObjectField(ref motion, "Motion", false);
                    ObjectField(ref rootGameObject, "Root GameObject", true);

                    using (new EditorGUI.DisabledScope(!motion))
                    {
                        if (GUILayout.Button("Parse") && motion && rootGameObject != null)
                        {
                            parsedRootObject = rootGameObject;
                            Container = new AnimationParser()
                                .ParseMotion(rootGameObject, motion, Utils.EmptyDictionary<AnimationClip, AnimationClip>());
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ObjectField<T>(ref T? value, string? label, bool allowScene) where T : Object
        {
            value = EditorGUILayout.ObjectField(label, value, typeof(T), allowScene) as T;
        }

        public enum ParserSource
        {
            WholeAvatar,
            AnimatorController,
            Motion
        }
    }
}
