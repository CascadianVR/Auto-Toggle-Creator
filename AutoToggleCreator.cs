#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public class AutoToggleCreator : EditorWindow
    {
        public static List<ToggleType> Toggles = new List<ToggleType>();
        public int TogglesCount;
        public GameObject selectedAvatar;
        public Animator myAnimator;
        AnimatorController controller;
        VRCExpressionParameters vrcParam;
        VRCExpressionsMenu vrcMenu;
        public string saveDir;
        private Vector2 scrollPos;
        private int animationType;
        
        private enum AnimationTypes
        {
            toggle,
            radial
        }

        private AnimationTypes animTypes;
    
        public class ToggleType
        {
            public string toggleName;
        
            public int toggleObjectCount;
            public List<GameObject> toggleObject;
            public List<bool> invertObject;
        
            public int shapekeyNameCount;
            public List<SkinnedMeshRenderer> shapekeyMesh;
            public List<int> shapekeyIndex;
            public List<string> shapekeyName;
            public List<bool> invertShapekey;

            public ToggleType()
            {
                toggleName = "Toggle Name";
            
                toggleObject = new List<GameObject>();
                toggleObjectCount = 0;
                invertObject = new List<bool>();
            
                shapekeyName = new List<string>();
                shapekeyMesh = new List<SkinnedMeshRenderer>();
                shapekeyIndex = new List<int>();
                shapekeyNameCount = 0;
                invertShapekey = new List<bool>();
            }
        }
    
    
        [MenuItem("Cascadian/AutoToggleCreator")]

        static void Init()
        {
            // Get existing open window or if none, make a new one:
            AutoToggleCreator window = (AutoToggleCreator)EditorWindow.GetWindow(typeof(AutoToggleCreator));
            window.Show();
            Toggles = new List<ToggleType>();
        }

        public void OnGUI()
        {
            UISetup();
            AutoFillSelection();
            EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true); // Disable controls until required assets are assigned
            HorizontalLine(Color.white, 10f);
            //DragInList();
            GroupList(); // Manages UI and Logic for grouping toggles and assigning propertis for each.
            GUILayout.FlexibleSpace();


            if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
            {
                CreateClips();      // Creates the Animation Clips needed for toggles.
                ApplyToAnimator();  // Handles making toggle bool property, layer setup, states and transitions.
                MakeVRCParameter(); // Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
                MakeVRCMenu();      // Adds the new toggles to the selected VRCMenu with appropriate settings
                Postprocessing();   // Makes sure to save all the newly created and modified assets
            }

            EditorGUI.EndDisabledGroup();

        }

        private void AutoFillSelection()
        {
            var style = new GUIStyle()
            {
                padding = new RectOffset(10,10,6,6),
            };
            
            var style2 = new GUIStyle()
            {
                normal = {background = Texture2D.grayTexture}
            };
            
            var vertStyle = new GUIStyle("window")
            {
                normal = { textColor = Color.white}, 
                //margin = new RectOffset(5,5,5,5),
                
                padding = new RectOffset(5,5,5,5),
            };
            
            GUILayout.BeginVertical(style);
            GUILayout.BeginHorizontal(style);
            GUILayout.BeginVertical(vertStyle);
            
            VRCAvatarDescriptor[] avatars = FindObjectsOfType<VRCAvatarDescriptor>();

            var buttonStyle = new GUIStyle
            {
                normal = { textColor = Color.white}, 
                active = { background = Texture2D.blackTexture, textColor = Color.cyan}, 
                fontSize = 14,
                padding = new RectOffset(5,5,5,5),
            };

            foreach (var avatar in avatars)
            {
                if (selectedAvatar != null && (avatar.gameObject.name == selectedAvatar.name))
                {
                    buttonStyle.normal.textColor = Color.cyan;
                    EditorGUILayout.BeginVertical(style2);
                    if(GUILayout.Button(avatar.name, buttonStyle)) getAvatarInfo(avatar);
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    buttonStyle.normal.textColor = Color.white;
                    if(GUILayout.Button(avatar.name, buttonStyle)) getAvatarInfo(avatar);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            
        }

        private void getAvatarInfo(VRCAvatarDescriptor avatar)
        {
            selectedAvatar = avatar.gameObject;
            Transform SelectedObj = avatar.transform;
            
            myAnimator = SelectedObj.GetComponent<Animator>();
            controller = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
            vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
            
            if (myAnimator== null) 
                Debug.LogWarning("Please make sure you have an Animator Component on your Avatar.");
            if (vrcMenu== null || vrcParam== null || controller == null) 
                Debug.LogWarning("Please make sure you have an FX Controller, VRC Parameter and VRC Menu assigned to your Avatar Descriptor.");
        }
        
        private void DragInList()
        {
            // Dragging over window
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                // To consume drag data.
                DragAndDrop.AcceptDrag();
    
                // GameObjects from hierarchy.
                if (DragAndDrop.paths.Length == 0 && DragAndDrop.objectReferences.Length > 0)
                {
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        Debug.Log("- " + obj);
                        TogglesCount++;
                        Toggles.Add(new ToggleType());
                        Toggles[Toggles.Count - 1].toggleObject.Add((GameObject)obj);
                    }
                }
            }
        }
        
        private void GroupList()
        {
            GUIStyle togglelabel = new GUIStyle("label") { fontSize = 18, alignment = TextAnchor.UpperCenter, contentOffset = new Vector2(28,0)};

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Animation Setup", togglelabel);
            //animTypes = (AnimationTypes)EditorGUILayout.EnumPopup(GUIContent.none, animTypes) ;

            if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                if (TogglesCount <= 0) {return;}
                TogglesCount--;
                Toggles.RemoveAt(Toggles.Count - 1);
            }

            if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                TogglesCount++;
                Toggles.Add(new ToggleType());
            }
            EditorGUILayout.EndHorizontal();
        
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            for (int i = 0; i < TogglesCount; i++)
            {
                GUIStyle nameStyle = new GUIStyle("textfield") {alignment = TextAnchor.UpperCenter, fontSize = 14};
                nameStyle.normal.textColor = Color.white;
                GUIStyle style = new GUIStyle("window") { fontStyle = FontStyle.Bold, margin = new RectOffset(5,5,5,5)};
                GUILayout.BeginVertical( style,  GUILayout.ExpandHeight(true));
                GUILayout.Space(-12);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Toggles[i].toggleName = GUILayout.TextField(Toggles[i].toggleName, nameStyle,  GUILayout.ExpandWidth(true), GUILayout.MinWidth(100f));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GameObjectList(ref Toggles[i].toggleObjectCount, ref Toggles[i].toggleObject, ref Toggles[i].invertObject);
                ShapekeyList(ref Toggles[i].shapekeyNameCount, ref Toggles[i].shapekeyMesh, ref Toggles[i].shapekeyIndex, ref Toggles[i].shapekeyName, ref Toggles[i].invertShapekey);
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        void GameObjectList(ref int count, ref List<GameObject> objects, ref List<bool> invert)
        {
            GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(0,0,5,0) };
            GUILayout.BeginVertical(GUIContent.none, layout,GUILayout.ExpandHeight(true));
            GUILayout.Space(-18);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                if (count <= 0) {return;}
                count--;
                objects.RemoveAt(objects.Count - 1);
                invert.RemoveAt(invert.Count - 1); 
            }
            if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
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
                    new GUIContent("Invert", "Unchecked: Automatically toggles to opposite state (Recommended)\nChecked: Toggles to current state (Usually not needed)"),
                    invert[i],
                    EditorStyles.boldLabel
                );
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        void ShapekeyList(ref int count, ref List<SkinnedMeshRenderer> mesh, ref List<int> index, ref List<string> shapekey, ref List<bool> invert)
        {
            GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(0,0,5,0)};
            GUILayout.BeginVertical(GUIContent.none,layout,GUILayout.ExpandHeight(true));
            GUILayout.Space(-18);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
            {
                if (count <= 0) {return;}
                count--;
                shapekey.RemoveAt(shapekey.Count - 1);
                invert.RemoveAt(invert.Count - 1);
                mesh.RemoveAt(mesh.Count - 1);
                index.RemoveAt(index.Count - 1);
            }
            if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
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
        
            for(int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                shapekeys.Add(renderer.sharedMesh.GetBlendShapeName(i));
            }
        
            return shapekeys.ToArray();
        }
    
        private void AutofFillSelection()
        {
            EditorGUILayout.Space(15);
            if (GUILayout.Button("Auto-Fill with Selected Avatar", GUILayout.Height(30f)))
            {
                if (Selection.activeTransform.GetComponent<Animator>() == null) { return; }
                Transform SelectedObj = Selection.activeTransform;
                myAnimator = SelectedObj.GetComponent<Animator>();
                controller = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
                vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
                vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
            }
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            //Avatar Animator
            GUILayout.Label("AVATAR ANIMATOR", EditorStyles.boldLabel);
            myAnimator = (Animator)EditorGUILayout.ObjectField(myAnimator, typeof(Animator), true, GUILayout.Height(40f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            //FX Animator Controller
            GUILayout.Label("FX AVATAR CONTROLLER", EditorStyles.boldLabel);
            controller = (AnimatorController)EditorGUILayout.ObjectField(controller, typeof(AnimatorController), true, GUILayout.Height(40f));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            //VRCExpressionParameters
            GUILayout.Label("VRC EXPRESSION PARAMETERS", EditorStyles.boldLabel);
            vrcParam = (VRCExpressionParameters)EditorGUILayout.ObjectField(vrcParam, typeof(VRCExpressionParameters), true, GUILayout.Height(40f));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            //VRCExpressionMenu
            GUILayout.Label("VRC EXPRESISON MENU", EditorStyles.boldLabel);
            vrcMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(vrcMenu, typeof(VRCExpressionsMenu), true, GUILayout.Height(40f));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

        }

        private void CreateClips()
        {
            foreach (var toggle in Toggles)
            {
                AnimationClip toggleClipOn = new AnimationClip() {legacy = false}; //Clip for ON
                AnimationClip toggleClipOff = new AnimationClip() {legacy = false}; //Clip for OFF

                for (int i = 0; i < toggle.toggleObject.Count; i++)
                {
                    bool isActive = toggle.toggleObject[i].activeSelf;
                
                    float toggleObjValue = !isActive ? 1f : 0f;
                    if (toggle.invertObject[i]) toggleObjValue = 1f-toggleObjValue;

                    toggleClipOn.SetCurve(
                        GetGameObjectPath(toggle.toggleObject[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(GameObject),
                        "m_IsActive",
                        new AnimationCurve(new Keyframe(0, toggleObjValue, 0, 0),
                            new Keyframe(0.016666668f, toggleObjValue, 0, 0))
                    );
                    toggleClipOff.SetCurve(
                        GetGameObjectPath(toggle.toggleObject[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(GameObject),
                        "m_IsActive",
                        new AnimationCurve(new Keyframe(0, 1f-toggleObjValue, 0, 0),
                            new Keyframe(0.016666668f, 1f-toggleObjValue, 0, 0))
                    );
                }
            
                for (int i = 0; i < toggle.shapekeyName.Count; i++) // For each shapekey add 
                {
                    float toggleShapeValue = toggle.shapekeyMesh[i].GetBlendShapeWeight(toggle.shapekeyMesh[i].sharedMesh.GetBlendShapeIndex(toggle.shapekeyName[i])) < 100f ? 100f : 0f;
                    if (toggle.invertShapekey[i]) toggleShapeValue = 100f-toggleShapeValue;

                    toggleClipOn.SetCurve(
                        GetGameObjectPath(toggle.shapekeyMesh[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + toggle.shapekeyName[i],
                        new AnimationCurve(new Keyframe(0, toggleShapeValue, 0, 0),
                            new Keyframe(0.016666668f, toggleShapeValue, 0, 0))
                    );
                    toggleClipOff.SetCurve(
                        GetGameObjectPath(toggle.shapekeyMesh[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + toggle.shapekeyName[i],
                        new AnimationCurve(new Keyframe(0, 100f-toggleShapeValue, 0, 0),
                            new Keyframe(0.016666668f, 100f-toggleShapeValue, 0, 0))
                    );
                }

                saveDir = AssetDatabase.GetAssetPath(controller);
                saveDir = saveDir.Substring(0, saveDir.Length - controller.name.Length - 11);

                //Check to see if path exists. If not, create it.
                if (!Directory.Exists(saveDir + "ToggleAnimations/"))
                {
                    Directory.CreateDirectory(saveDir + "ToggleAnimations/");
                }

                //Save on animation clips
                AssetDatabase.CreateAsset(toggleClipOn, saveDir + "ToggleAnimations/" + $"On{toggle.toggleName}.anim");
                AssetDatabase.CreateAsset(toggleClipOff, saveDir + "ToggleAnimations/" + $"Off{toggle.toggleName}.anim");
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyToAnimator()
        {
            foreach (var toggle in Toggles)
            {
                // Check if the parameter or layer already exists. If not, add parameter
                bool existParam = doesNameExistParam(toggle.toggleName + "Toggle", controller.parameters);
                if (existParam == false)
                {
                    controller.AddParameter(toggle.toggleName + "Toggle", AnimatorControllerParameterType.Bool);
                }

                //Check if a layer already Exists with that name. If so, remove and add new one.
                bool existLayer = doesNameExistLayer(toggle.toggleName, controller.layers);
                if (existLayer)
                {
                    controller.RemoveLayer(controller.layers.Length-1);
                }
            
                controller.AddLayer(toggle.toggleName);

                AnimatorController animatorController = controller;
                var sm = animatorController.layers[controller.layers.Length - 1].stateMachine;

                sm.AddState("stateIdle", new Vector3(300, 0, 0));
                sm.AddState("stateOn", new Vector3(300, -220, 0));
                sm.AddState("stateOff", new Vector3(100, -110, 0));

                sm.states[0].state.name = "IDLE" + toggle.toggleName;
                sm.states[0].state.motion = null;
                sm.states[0].state.writeDefaultValues = true;
            
                sm.states[1].state.name = "ON" + toggle.toggleName;
                sm.states[1].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion));
                sm.states[1].state.writeDefaultValues = false;
            
                sm.states[2].state.name = "OFF" + toggle.toggleName;
                sm.states[2].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion));
                sm.states[2].state.writeDefaultValues = false;

                sm.states[0].state.AddTransition(sm.states[1].state);
                sm.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.If, 0, toggle.toggleName + "Toggle");
                sm.states[1].state.AddTransition(sm.states[2].state);
                sm.states[1].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 0, toggle.toggleName + "Toggle");
                sm.states[2].state.AddTransition(sm.states[0].state);
                sm.states[2].state.transitions[0].hasExitTime = true;

                //Set Layer Weight
                AnimatorControllerLayer[] layers = controller.layers;
                layers[controller.layers.Length - 1].defaultWeight = 1;
                controller.layers = layers;
            }
        }

        private void MakeVRCParameter()
        {
            VRCExpressionParameters.Parameter[] newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + Toggles.Count];
            int k = 0;
            foreach (var toggle in Toggles)
            {
                //Add parameters that were already on the SO
                for (int i = 0; i < vrcParam.parameters.Length; i++)
                {
                    newList[i] = vrcParam.parameters[i];
                }

                bool same = false;
            
                //Make new parameter to add to list
                VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter();

                int vrcParamLength = vrcParam.parameters.Length;

                //Modify parameter according to user settings and object name
                newParam.name = toggle.toggleName + "Toggle";
                newParam.valueType = VRCExpressionParameters.ValueType.Bool;
                newParam.defaultValue = 0;
            
                //This garbage here checks to see if there is already a parameter with the same name. If so, It ignore it and removes one slip from the predetermined list.
                for (int j = 0; j < vrcParamLength; j++)
                {
                    if (newList[j].name == toggle.toggleName + "Toggle")
                    {
                        same = true;
                    }
                }

                //If no name name was found, then add parameter to list
                if (!same)
                {
                    newList[vrcParam.parameters.Length + k] = newParam;
                }

                k++;
            }
            //Apply new list to VRCExpressionParameter asset
            vrcParam.parameters = newList;
        }

        private void MakeVRCMenu()
        {

            foreach (var toggle in Toggles)
            {
                VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control();

                controlItem.name = toggle.toggleName;
                controlItem.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                controlItem.parameter = new VRCExpressionsMenu.Control.Parameter { name = toggle.toggleName + "Toggle" };
            
                for (int i = 0; i < vrcMenu.controls.Count; i++)
                {
                    if (vrcMenu.controls[i].name == controlItem.name)
                    {
                        vrcMenu.controls.RemoveAt(i);
                    }
                }
                vrcMenu.controls.Add(controlItem);
            }
        }

        private void Postprocessing()
        {
            AssetDatabase.Refresh();
        
            EditorUtility.SetDirty(vrcParam);
            EditorUtility.SetDirty(vrcMenu);

            AssetDatabase.SaveAssets();
        }

        private bool doesNameExistParam(string paramName, AnimatorControllerParameter[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].name == paramName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool doesNameExistLayer(string layerName, AnimatorControllerLayer[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].name == layerName)
                {
                    return true;
                }
            }
            return false;
        }

        private string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    
        void OnEnable()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
    
        public void OnAfterAssemblyReload() 
        {
            Debug.Log("After Assembly Reload");
            TogglesCount = 0;
        }

        private GUIStyle horizontalLine;
        private GUIStyle customButton;
        void UISetup()
        {
            horizontalLine = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 4, 4),
                fixedHeight = 1
            };
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        
            customButton = new GUIStyle("button") { fontSize = 20, contentOffset = new Vector2(0.5f,-0.5f), padding = new RectOffset(0,0,0,0), margin = new RectOffset(0,0,0,0)};
        }
    
        void HorizontalLine ( Color color, float spacing) {
            GUILayout.Space(spacing);
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box( GUIContent.none, horizontalLine );
            GUI.color = c;
            GUILayout.Space(spacing);
        }
    
    }
}

#endif
