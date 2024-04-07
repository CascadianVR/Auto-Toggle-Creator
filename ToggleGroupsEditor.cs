#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public static class ToggleGroupsEditor
    {

        private static Texture2D plusIcon;
        private static Texture2D minusIcon;
        private static Texture2D buttontex;
        private static Vector2 scrollPos;
        public static Texture2D red;
        public static Texture2D darkred;
        public static Texture2D green;
        public static Texture2D darkgreen;
        

        public static void GroupList()
        {
            plusIcon = AutoToggleCreator.plusIcon;
            minusIcon = AutoToggleCreator.minusIcon;
            
            GUIStyle togglelabel = new GUIStyle("label") { fontSize = 18, alignment = TextAnchor.UpperCenter, contentOffset = new Vector2(28, 0) };

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Toggle Setup", togglelabel);
            
            GUIStyle toggleGroupButtonStyle = new GUIStyle("button");
            
            GUILayout.FlexibleSpace();

            if (AutoToggleCreator.CombineToggles) {
                toggleGroupButtonStyle.normal.background = green;
                toggleGroupButtonStyle.active.background = darkgreen;
            }
            else {
                toggleGroupButtonStyle.normal.background = red;
                toggleGroupButtonStyle.active.background = darkred;
            }
            
            if (GUILayout.Button("Combine All", toggleGroupButtonStyle, GUILayout.Width(80.0f)))
            {
                AutoToggleCreator.CombineToggles = !AutoToggleCreator.CombineToggles;
            }
            
            GUIStyle nameStyle = new GUIStyle("textfield")
            {
                fontSize = 14,
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUI.BeginDisabledGroup(!AutoToggleCreator.CombineToggles);
            AutoToggleCreator.CombineName = GUILayout.TextField(
                AutoToggleCreator.CombineName,
                nameStyle, 
                GUILayout.ExpandWidth(true), 
                GUILayout.MinWidth(120), 
                GUILayout.ExpandWidth(false)
            );
            EditorGUI.EndDisabledGroup();


            if (GUILayout.Button(minusIcon, GUILayout.Width(25), GUILayout.Height(25)))
            {
                if (AutoToggleCreator.Toggles.Count <= 0) { return; }

                AutoToggleCreator.Toggles.RemoveAt(AutoToggleCreator.Toggles.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(25), GUILayout.Height(25)))
            {
                AutoToggleCreator.Toggles.Add(new AutoToggleCreator.AvatarToggle());
            }

            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            for (int i = 0; i < AutoToggleCreator.Toggles.Count; i++)
            {
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
                GUILayout.Space(8);
                AutoToggleCreator.Toggles[i].toggleName = GUILayout.TextField(
                    AutoToggleCreator.Toggles[i].toggleName,
                    nameStyle, 
                    GUILayout.ExpandWidth(true), 
                    GUILayout.MinWidth(120), 
                    GUILayout.ExpandWidth(false)
                );
                
                GUILayout.FlexibleSpace();
                
                EditorGUIUtility.labelWidth = 65;
                GUILayout.Label("VRC Menu");
                List<string> names = new List<string>();
                VRCExpressionsMenu[] menus = new VRCExpressionsMenu[0];
                if (AutoToggleCreator.vrcMenu != null)
                    menus = GetVRCMenus(AutoToggleCreator.vrcMenu, ref names);
                AutoToggleCreator.Toggles[i].vrcMenuIndex = EditorGUILayout.Popup(AutoToggleCreator.Toggles[i].vrcMenuIndex , names.ToArray());
                AutoToggleCreator.Toggles[i].expressionMenu = menus[AutoToggleCreator.Toggles[i].vrcMenuIndex];
                EditorGUIUtility.labelWidth = 0;

                if (names.Count <= 0)
                {
                    GUILayout.Label("No available menus! They are likely all full.");
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    AutoToggleCreator.Toggles.RemoveAt(i);
                    GUIUtility.ExitGUI();
                    return;
                }
                
                GUILayout.Space(8);

                GUILayout.EndHorizontal();
                GameObjectList(
                    ref AutoToggleCreator.Toggles[i].toggleObjectCount,
                    ref AutoToggleCreator.Toggles[i].toggleObject,
                    ref AutoToggleCreator.Toggles[i].objectOffStates,
                    ref AutoToggleCreator.Toggles[i].objectOnStates
                );
                ShapekeyList(
                    ref AutoToggleCreator.Toggles[i].toggleShapekeyCount,
                    ref AutoToggleCreator.Toggles[i].shapekeyMesh,
                    ref AutoToggleCreator.Toggles[i].shapekeyIndex,
                    ref AutoToggleCreator.Toggles[i].shapekeyName,
                    ref AutoToggleCreator.Toggles[i].shapekeyOffStates,
                    ref AutoToggleCreator.Toggles[i].shapekeyOnStates
                );
                //GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                //GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();
        }

        static void GameObjectList(ref int count, ref List<GameObject> objects, ref List<bool> onStates, ref List<bool> offStates)
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
                offStates.RemoveAt(offStates.Count - 1);
                onStates.RemoveAt(onStates.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                count++;
                objects.Add(null);
                offStates.Add(false);
                onStates.Add(true);
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
                
                
                GUILayout.Space(10);
                
                GUILayout.Label("Off State:");
                offStates[i] = GUILayout.Toggle(offStates[i], "");
                
                GUILayout.Space(10);
                
                GUILayout.Label("On State:");
                onStates[i] = GUILayout.Toggle(onStates[i], "");
                
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            
        }

        static void ShapekeyList(ref int count, ref List<SkinnedMeshRenderer> mesh, ref List<int> index, ref List<string> shapekey,
            ref List<float> onStates, ref List<float> offStates)
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
                mesh.RemoveAt(mesh.Count - 1);
                index.RemoveAt(index.Count - 1);
                offStates.RemoveAt(offStates.Count - 1);
                onStates.RemoveAt(onStates.Count - 1);
            }

            if (GUILayout.Button(plusIcon, GUILayout.Width(20), GUILayout.Height(20)))
            {
                count++;
                shapekey.Add(null);
                mesh.Add(null);
                index.Add(0);
                offStates.Add(0);
                onStates.Add(100);
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
                        index[i] = EditorGUILayout.Popup(index[i], GetShapekeys(mesh[i]),GUILayout.Width(80));
                        shapekey[i] = mesh[i].sharedMesh.GetBlendShapeName(index[i]);
                        GUILayout.Space(5);
                        
                        GUILayout.Label("Off State:");
                        offStates[i] = EditorGUILayout.Slider(offStates[i], 0, 100);
                
                        GUILayout.Space(10);
                
                        GUILayout.Label("On State:");
                        onStates[i] = EditorGUILayout.Slider(onStates[i], 0, 100);
                        
                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        GUILayout.Label("No Blendshapes");
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        static string[] GetShapekeys(SkinnedMeshRenderer renderer)
        {
            List<string> shapekeys = new List<string>();

            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                shapekeys.Add(renderer.sharedMesh.GetBlendShapeName(i));
            }

            return shapekeys.ToArray();
        }

        static public VRCExpressionsMenu[] GetVRCMenus(VRCExpressionsMenu mainMenu, ref List<string> names)
        {
            List<VRCExpressionsMenu> menus = new List<VRCExpressionsMenu>();
            menus.Add(mainMenu);

            CheckMenu(mainMenu, ref menus);

            foreach (var menu in menus)
            {
                if (menu.controls.Count < 8) names.Add(menu.name);
            }
            
            return menus.ToArray();
        }

        static void CheckMenu(VRCExpressionsMenu currentMenu, ref List<VRCExpressionsMenu> menus)
        {
            for (int i = 0; i < currentMenu.controls.Count; i++)
            {
                if (currentMenu.controls[i].type == VRCExpressionsMenu.Control.ControlType.SubMenu)
                {
                    if (currentMenu.controls[i].subMenu == null) continue;
                    menus.Add(currentMenu.controls[i].subMenu);
                    CheckMenu(currentMenu.controls[i].subMenu, ref menus);
                }
            }
        }
    }
}
#endif