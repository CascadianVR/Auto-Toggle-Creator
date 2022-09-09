using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public class ToggleGroupsEditor
    {
        private Texture2D plusIcon;
        private Texture2D minusIcon;
        private Vector2 scrollPos;

        public void GroupList()
        {
            plusIcon = AutoToggleCreator.plusIcon;
            minusIcon = AutoToggleCreator.minusIcon;
            
            GUIStyle togglelabel = new GUIStyle("label") { fontSize = 18, alignment = TextAnchor.UpperCenter, contentOffset = new Vector2(28, 0) };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Toggle Setup", togglelabel);

            if (GUILayout.Button(minusIcon, GUILayout.Width(25), GUILayout.Height(25)))
            {
                if (AutoToggleCreator.Toggles.Count <= 0) { return; }

                AutoToggleCreator.Toggles.RemoveAt(AutoToggleCreator.Toggles.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(25), GUILayout.Height(25)))
            {
                AutoToggleCreator.Toggles.Add(new AutoToggleCreator.ToggleType());
            }

            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            for (int i = 0; i < AutoToggleCreator.Toggles.Count; i++)
            {
                GUIStyle nameStyle = new GUIStyle("textfield")
                {
                    fontSize = 14,
                    normal =
                    {
                        textColor = Color.white
                    },
                    alignment = TextAnchor.MiddleCenter
                };
                GUIStyle style = new GUIStyle("window")
                {
                    fontStyle = FontStyle.Bold, 
                    margin = new RectOffset(5, 5, 5, 5)
                };
                GUILayout.BeginVertical(
                    style, 
                    GUILayout.ExpandHeight(true)
                );
                GUILayout.Space(-12);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                AutoToggleCreator.Toggles[i].toggleName = GUILayout.TextField(
                    AutoToggleCreator.Toggles[i].toggleName,
                    nameStyle, 
                    GUILayout.ExpandWidth(true), 
                    GUILayout.MinWidth(120), 
                    GUILayout.ExpandWidth(false)
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Space(7);
                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 65;
                AutoToggleCreator.Toggles[i].expressionMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(
                    new GUIContent("VRC Menu", "Which menu to assign the toggle to. By default uses the main menu"),
                    AutoToggleCreator.Toggles[i].expressionMenu,
                    typeof(VRCExpressionsMenu),
                    true,
                    GUILayout.Height(20f),
                    GUILayout.Width(200f)
                );
                EditorGUIUtility.labelWidth = 0;
                GUILayout.FlexibleSpace();
                AutoToggleCreator.Toggles[i].groupObject = EditorGUILayout.ToggleLeft(
                    new GUIContent("Group Toggle",
                        "Every toggle marked with this will be added to the same layer, making the current toggle overwrite the previous toggle form the group."),
                    AutoToggleCreator.Toggles[i].groupObject,
                    GUILayout.Width(100f)
                );
                GUILayout.FlexibleSpace();
                
                GUILayout.EndHorizontal();
                GameObjectList(
                    ref AutoToggleCreator.Toggles[i].toggleObjectCount, 
                    ref AutoToggleCreator.Toggles[i].toggleObject, 
                    ref AutoToggleCreator.Toggles[i].invertObject
                );
                ShapekeyList(
                    ref AutoToggleCreator.Toggles[i].shapekeyNameCount, 
                    ref AutoToggleCreator.Toggles[i].shapekeyMesh, 
                    ref AutoToggleCreator.Toggles[i].shapekeyIndex, 
                    ref AutoToggleCreator.Toggles[i].shapekeyName, 
                    ref AutoToggleCreator.Toggles[i].invertShapekey
                );
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
        }

        void GameObjectList(ref int count, ref List<GameObject> objects, ref List<bool> invert)
        {
            GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(10, 10, 10, 10) };
            GUILayout.BeginVertical(GUIContent.none, layout, GUILayout.ExpandHeight(true));
            GUILayout.Space(-18);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(minusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (count <= 0) { return; }

                count--;
                objects.RemoveAt(objects.Count - 1);
                invert.RemoveAt(invert.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                count++;
                objects.Add(null);
                invert.Add(false);
            }

            GUILayout.Label("GameObjects");
            GUILayout.EndHorizontal();

            for (int i = 0; i < count; i++)
            {
                GUILayout.BeginHorizontal();
                objects[i] = (GameObject)EditorGUILayout.ObjectField(
                    objects[i],
                    typeof(GameObject),
                    true,
                    GUILayout.Height(20f),
                    GUILayout.Width(150f)
                );
                
                invert[i] = EditorGUILayout.ToggleLeft(
                    new GUIContent("Invert","Use current state as \"Active\" state."),
                    invert[i],
                    EditorStyles.boldLabel
                );
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        void ShapekeyList(ref int count, ref List<SkinnedMeshRenderer> mesh, ref List<int> index, ref List<string> shapekey, ref List<bool> invert)
        {
            GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(10, 10, 10, 10) };
            GUILayout.BeginVertical(GUIContent.none, layout, GUILayout.ExpandHeight(true));
            GUILayout.Space(-18);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(minusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                if (count <= 0) { return; }

                count--;
                shapekey.RemoveAt(shapekey.Count - 1);
                invert.RemoveAt(invert.Count - 1);
                mesh.RemoveAt(mesh.Count - 1);
                index.RemoveAt(index.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                count++;
                shapekey.Add(null);
                invert.Add(false);
                mesh.Add(null);
                index.Add(0);
            }

            GUILayout.Label("Blendshapes");
            GUILayout.EndHorizontal();

            for (int i = 0; i < count; i++)
            {
                GUILayout.BeginHorizontal();
                mesh[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    mesh[i],
                    typeof(SkinnedMeshRenderer),
                    true,
                    GUILayout.Height(20f),
                    GUILayout.Width(150f)
                );
                if (mesh[i] != null)
                {
                    if (mesh[i].sharedMesh.blendShapeCount > 0)
                    {
                        index[i] = EditorGUILayout.Popup(index[i], GetShapekeys(mesh[i]));
                        shapekey[i] = mesh[i].sharedMesh.GetBlendShapeName(index[i]);
                        invert[i] = EditorGUILayout.ToggleLeft(
                            "Invert",
                            invert[i],
                            EditorStyles.boldLabel
                        );
                    }
                    else
                    {
                        GUILayout.Label("No blendshapes on mesh!");
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        string[] GetShapekeys(SkinnedMeshRenderer renderer)
        {
            List<string> shapekeys = new List<string>();

            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                shapekeys.Add(renderer.sharedMesh.GetBlendShapeName(i));
            }

            return shapekeys.ToArray();
        }
    }
}