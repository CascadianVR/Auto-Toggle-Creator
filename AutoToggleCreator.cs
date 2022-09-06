using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using UnityEditor.Animations;

public class AutoToggleCreator : EditorWindow
{
    public static List<ToggleType> Toggles = new List<ToggleType>();
    public int TogglesCount = 0;
    public string[] toggleShapekeys = {"testc"};
    public Animator myAnimator;
    AnimatorController controller;
    VRCExpressionParameters vrcParam;
    VRCExpressionsMenu vrcMenu;
    public SkinnedMeshRenderer ShapekeyMesh;
    static bool parameterSave;
    static bool defaultOn;
    static bool defaultShapeOn;
    static bool seperateToggles;
    static bool usingObjects;
    static bool usingShapekeys;
    public string saveDir;
    private Vector2 scrollPos;
    public int stringIndex = 0;
    private GameObject[] toggleObjects;
    
    public class ToggleType
    {
        public int toggleObjectCount;
        public List<GameObject> toggleObject;
        public List<bool> invertObject;
        
        public int shapekeyNameCount;
        public List<SkinnedMeshRenderer> shapekeyMesh;
        public List<string> shapekeyName;
        public List<bool> invertShapekey;

        public ToggleType()
        {
            toggleObject = new List<GameObject>();
            toggleObjectCount = 0;
            invertObject = new List<bool>();
            
            shapekeyName = new List<string>();
            shapekeyMesh = new List<SkinnedMeshRenderer>();
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
    
    }

    public void OnGUI()
    {
        UISetup();
        
        AutoFillSelection();
        EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true); // Disable controls until required assets are assigned

        HorizontalLine(Color.white, 10f);

        GroupList(); // Manages UI and Logic for grouping toggles and assigning propertis for each.
         
        HorizontalLine(Color.white, 10f);

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

    List<int> toggleNum = new List<int>();
    private void GroupList()
    {
        GUIStyle customButton = new GUIStyle("button") { fontSize = 20 };
        GUIStyle togglelabel = new GUIStyle("label") { fontSize = 18, alignment = TextAnchor.UpperCenter, contentOffset = new Vector2(28,0)};

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Toggle Setup", togglelabel);

        if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
        {
            if (TogglesCount <= 0) {return;}
            TogglesCount--;
            Toggles.RemoveAt(Toggles.Count - 1);
            toggleNum.RemoveAt(toggleNum.Count - 1);
        }

        if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
        {
            TogglesCount++;
            Toggles.Add(new ToggleType());
            toggleNum.Add(toggleNum.Count);
        }
        EditorGUILayout.EndHorizontal();
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos ,  GUILayout.ExpandHeight(true));
        for (int i = 0; i < TogglesCount; i++)
        {
            GUIStyle style = new GUIStyle("window") { fontStyle = FontStyle.Bold, margin = new RectOffset(5,5,5,5)};
            GUILayout.BeginVertical("Toggle " + (toggleNum[i] + 1), style,  GUILayout.ExpandHeight(true));
            GameObjectList(ref Toggles[i].toggleObjectCount, ref Toggles[i].toggleObject, ref Toggles[i].invertObject);
            ShapekeyList(ref Toggles[i].shapekeyNameCount, ref Toggles[i].shapekeyMesh, ref Toggles[i].shapekeyName, ref Toggles[i].invertShapekey);
            GUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    void GameObjectList(ref int count, ref List<GameObject> objects, ref List<bool> invert)
    {
        GUIStyle customButton = new GUIStyle("button") { fontSize = 20 };
        
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
        GUILayout.Label("Objects");
        GUILayout.EndHorizontal();

        GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(10,10,10,10) };
        GUILayout.BeginVertical(layout,GUILayout.ExpandHeight(true));
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
                "Invert",
                invert[i],
                EditorStyles.boldLabel
            );
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }
    
    void ShapekeyList(ref int count, ref List<SkinnedMeshRenderer> mesh, ref List<string> shapekey, ref List<bool> invert)
    {
        GUIStyle customButton = new GUIStyle("button") { fontSize = 20 };
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
        {
            if (count <= 0) {return;}
            count--;
            shapekey.RemoveAt(shapekey.Count - 1);
            invert.RemoveAt(invert.Count - 1);
            mesh.RemoveAt(mesh.Count - 1);
        }
        if (GUILayout.Button("+", customButton, GUILayout.Width(25f), GUILayout.Height(25f)))
        {
            count++;
            shapekey.Add(null);
            invert.Add(false);
            mesh.Add(null);
        }
        GUILayout.Label("Shapekeys");
        GUILayout.EndHorizontal();
        
        GUIStyle layout = new GUIStyle("window") { margin = new RectOffset(10,10,10,10) };
        GUILayout.BeginVertical(layout,GUILayout.ExpandHeight(true));
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
                stringIndex = EditorGUILayout.Popup(stringIndex, GetShapekeys(mesh[i]));
                shapekey[i] = mesh[i].sharedMesh.GetBlendShapeName(stringIndex);
            }
            invert[i] = EditorGUILayout.ToggleLeft(
                "Invert",
                invert[i],
                EditorStyles.boldLabel
            );
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
    
    private void AutoFillSelection()
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
        if (!seperateToggles)
        {
            AnimationClip toggleClipOn = new AnimationClip(); //Clip for ON
            
            float toggleValue = 1;
            if (defaultOn == true) toggleValue = 0f;
            else toggleValue = 1f;
            
            float toggleShapeValue = 100;
            if (defaultShapeOn == true) toggleShapeValue = 0f;
            else toggleShapeValue = 100f;

            toggleClipOn.legacy = false;

            if (usingObjects)
            {
                for (int i = 0; i < toggleObjects.Length; i++)
                {
                    toggleClipOn.SetCurve
                    (GetGameObjectPath(toggleObjects[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(GameObject),
                        "m_IsActive",
                        new AnimationCurve(new Keyframe(0, toggleValue, 0, 0),
                            new Keyframe(0.016666668f, toggleValue, 0, 0))
                    );
                }
            }

            if (usingShapekeys)
            {
                for (int i = 0; i < toggleShapekeys.Length; i++)
                {
                    if (toggleShapekeys.Length >= i + 1)
                    {
                        toggleClipOn.SetCurve
                        (GetGameObjectPath(ShapekeyMesh.transform).Substring(myAnimator.gameObject.name.Length + 1),
                            typeof(SkinnedMeshRenderer),
                            "blendShape." + toggleShapekeys[i],
                            new AnimationCurve(new Keyframe(0, toggleShapeValue, 0, 0),
                                new Keyframe(0.016666668f, toggleShapeValue, 0, 0))
                        );
                    }
                }
            }

            saveDir = AssetDatabase.GetAssetPath(controller);
            saveDir = saveDir.Substring(0, saveDir.Length - controller.name.Length - 11);

            //Check to see if path exists. If not, create it.
            if (!Directory.Exists(saveDir + "ToggleAnimations/"))
            {
                Directory.CreateDirectory(saveDir + "ToggleAnimations/");
            }

            //Save on animation clips (Off should not be needed?)
            AssetDatabase.CreateAsset(toggleClipOn, saveDir + "ToggleAnimations/" + $"On{toggleObjects[0].name}Group.anim");
            //AssetDatabase.CreateAsset(toggleClipOff, $"Assets/ToggleAnimations/{myAnimator.gameObject.name}/Off{toggleObjects[i].name}.anim");
            AssetDatabase.SaveAssets();
        }
        else
        {
            for (int i = 0; i < toggleObjects.Length; i++)
            {
                //Make animation clips for on and off state and set curves for game objects on and off
                AnimationClip toggleClipOn = new AnimationClip(); //Clip for ON

                float toggleValue = 1;
                if (defaultOn == true) toggleValue = 0f;
                else toggleValue = 1f;

                float toggleShapeValue = 100;
                if (defaultShapeOn == true) toggleShapeValue = 0f;
                else toggleShapeValue = 100f;

                toggleClipOn.legacy = false;
                toggleClipOn.SetCurve
                (GetGameObjectPath(toggleObjects[i].transform).Substring(myAnimator.gameObject.name.Length + 1),
                    typeof(GameObject),
                    "m_IsActive",
                    new AnimationCurve(new Keyframe(0, toggleValue, 0, 0),
                        new Keyframe(0.016666668f, toggleValue, 0, 0))
                );

                if (toggleShapekeys.Length >= i + 1)
                {
                    toggleClipOn.SetCurve
                    (GetGameObjectPath(ShapekeyMesh.transform).Substring(myAnimator.gameObject.name.Length + 1),
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + toggleShapekeys[i],
                        new AnimationCurve(new Keyframe(0, toggleShapeValue, 0, 0),
                            new Keyframe(0.016666668f, toggleShapeValue, 0, 0))
                    );
                }

                saveDir = AssetDatabase.GetAssetPath(controller);
                saveDir = saveDir.Substring(0, saveDir.Length - controller.name.Length - 11);

                //Check to see if path exists. If not, create it.
                if (!Directory.Exists(saveDir + "ToggleAnimations/"))
                {
                    Directory.CreateDirectory(saveDir + "ToggleAnimations/");
                }

                //Save on animation clips (Off should not be needed?)
                AssetDatabase.CreateAsset(toggleClipOn, saveDir + "ToggleAnimations/" + $"On{toggleObjects[i].name}.anim");
                //AssetDatabase.CreateAsset(toggleClipOff, $"Assets/ToggleAnimations/{myAnimator.gameObject.name}/Off{toggleObjects[i].name}.anim");
                AssetDatabase.SaveAssets();

            }
        }
    }

    private void ApplyToAnimator()
    {
        if (!seperateToggles)
        {
            bool existParam = doesNameExistParam(toggleObjects[0].name + "ToggleGroup", controller.parameters);
            bool existLayer = doesNameExistLayer(toggleObjects[0].name, controller.layers);
            
            //Check if a parameter already Ixists with that name. If so, Ignore adding parameter.
            if (existParam == false)
            {
                controller.AddParameter(toggleObjects[0].name + "ToggleGroup", UnityEngine.AnimatorControllerParameterType.Bool);
            }

            //Check if a layer already Ixists with that name. If so, Ignore adding layer.
            if (existLayer == false)
            {
                controller.AddLayer(toggleObjects[0].name + "Group");

                //Creating On and Off(Empty) states
                AnimatorState stateOn = new AnimatorState();
                stateOn.name = "ON";
                stateOn.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggleObjects[0].name}Group.anim", typeof(Motion));
                AnimatorState stateOff = new AnimatorState();
                stateOff.name = "OFF";
                stateOff.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggleObjects[0].name}Group.anim", typeof(Motion));

                //Adding created states to controller layer
                controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOff, new Vector3(0, 1, 0));
                controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOn, new Vector3(0, 3, 0));

                //Transition states
                AnimatorStateTransition OnOff = new AnimatorStateTransition();
                OnOff.name = "OnOff";
                OnOff.AddCondition(AnimatorConditionMode.If, 0, toggleObjects[0].name + "ToggleGroup");
                OnOff.destinationState = controller.layers[controller.layers.Length - 1].stateMachine.states[1].state;
                AnimatorStateTransition OffOn = new AnimatorStateTransition();
                OffOn.name = "OffOn";
                OffOn.AddCondition(AnimatorConditionMode.IfNot, 0, toggleObjects[0].name + "ToggleGroup");
                OffOn.destinationState = controller.layers[controller.layers.Length - 1].stateMachine.states[0].state;

                //If True, go to ON state.
                controller.layers[controller.layers.Length - 1].stateMachine.AddAnyStateTransition(OffOn.destinationState);
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[0].AddCondition(AnimatorConditionMode.IfNot, 0,
                    toggleObjects[0].name + "ToggleGroup");
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[0].duration = 0.1f;

                //If False, go to Off (Empty) state.
                controller.layers[controller.layers.Length - 1].stateMachine.AddAnyStateTransition(OnOff.destinationState);
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].AddCondition(AnimatorConditionMode.If, 0,
                    toggleObjects[0].name + "ToggleGroup");
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].duration = 0.1f;

                AssetDatabase.AddObjectToAsset(stateOn, AssetDatabase.GetAssetPath(controller));
                AssetDatabase.AddObjectToAsset(stateOff, AssetDatabase.GetAssetPath(controller));
                AssetDatabase.SaveAssets();

            }

            //Set Layer Weight
            UnityEditor.Animations.AnimatorControllerLayer[] layers = controller.layers;
            layers[controller.layers.Length - 1].defaultWeight = 1;
            controller.layers = layers;
        }
        
        else 
        {
            for (int i = 0; i < toggleObjects.Length; i++)
            {
                bool existParam = doesNameExistParam(toggleObjects[i].name + "Toggle", controller.parameters);
                bool existLayer = doesNameExistLayer(toggleObjects[i].name, controller.layers);

                //Check if a parameter already Ixists with that name. If so, Ignore adding parameter.
                if (existParam == false)
                {
                    controller.AddParameter(toggleObjects[i].name + "Toggle", UnityEngine.AnimatorControllerParameterType.Bool);
                }

                //Check if a layer already Ixists with that name. If so, Ignore adding layer.
                if (existLayer == false)
                {
                    controller.AddLayer(toggleObjects[i].name.Replace(".", "_"));

                    //Creating On and Off(Empty) states
                    AnimatorState stateOn = new AnimatorState();
                    stateOn.name = "ON";
                    stateOn.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggleObjects[i].name}.anim", typeof(Motion));
                    AnimatorState stateOff = new AnimatorState();
                    stateOff.name = "OFF";
                    stateOff.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggleObjects[i].name}.anim", typeof(Motion));

                    //Adding created states to controller layer
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOff, new Vector3(0, 1, 0));
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOn, new Vector3(0, 3, 0));

                    //Transition states
                    AnimatorStateTransition OnOff = new AnimatorStateTransition();
                    OnOff.name = "OnOff";
                    OnOff.AddCondition(AnimatorConditionMode.If, 0, toggleObjects[i].name + "Toggle");
                    OnOff.destinationState = controller.layers[controller.layers.Length - 1].stateMachine.states[1].state;
                    AnimatorStateTransition OffOn = new AnimatorStateTransition();
                    OffOn.name = "OffOn";
                    OffOn.AddCondition(AnimatorConditionMode.IfNot, 0, toggleObjects[i].name + "Toggle");
                    OffOn.destinationState = controller.layers[controller.layers.Length - 1].stateMachine.states[0].state;

                    //If True, go to ON state.
                    controller.layers[controller.layers.Length - 1].stateMachine.AddAnyStateTransition(OffOn.destinationState);
                    controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[0].AddCondition(AnimatorConditionMode.IfNot, 0,
                        toggleObjects[i].name + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[0].duration = 0.1f;

                    //If False, go to Off (Empty) state.
                    controller.layers[controller.layers.Length - 1].stateMachine.AddAnyStateTransition(OnOff.destinationState);
                    controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].AddCondition(AnimatorConditionMode.If, 0,
                        toggleObjects[i].name + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].duration = 0.1f;

                    AssetDatabase.AddObjectToAsset(stateOn, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.AddObjectToAsset(stateOff, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.SaveAssets();

                }

                //Set Layer Weight
                UnityEditor.Animations.AnimatorControllerLayer[] layers = controller.layers;
                layers[controller.layers.Length - 1].defaultWeight = 1;
                controller.layers = layers;

            }
        }
    }

    private void MakeVRCParameter()
    {
        VRCExpressionParameters.Parameter[] newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + toggleObjects.Length];
        
        //Add parameters that were already on the SO
        for (int i = 0; i < vrcParam.parameters.Length; i++)
        {
            newList[i] = vrcParam.parameters[i];
        }
        
        bool same = false;

        if (!seperateToggles)
        {
            //Make new parameter to add to list
            VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter();

            int vrcParapLength = vrcParam.parameters.Length;

            //Modify parameter according to user settings and object name
            newParam.name = toggleObjects[0].name + "ToggleGroup";
            newParam.valueType = VRCExpressionParameters.ValueType.Bool;
            newParam.defaultValue = 0;

            //Check to see if parameter is saved
            if (parameterSave == true) { newParam.saved = true; } else { newParam.saved = false; }

            same = false;

            //THis garbage here checks to see if there is already a parameter with the same name. If so, It ignore it and removes one slip from the predetermined list.
            for (int j = 0; j < vrcParapLength; j++)
            {
                if (newList[j].name == toggleObjects[0].name + "ToggleGroup")
                {
                    same = true;
                    newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + toggleObjects.Length - 1];

                    for (int k = 0; k < vrcParam.parameters.Length; k++)
                    {
                        newList[k] = vrcParam.parameters[k];
                    }
                }
            }

            //If no name name was found, then add parameter to list
            if (same == false) {
                newList[vrcParam.parameters.Length] = newParam;
            }
        }

        else
        {
            for (int i = 0; i < toggleObjects.Length; i++)
            {
                //Make new parameter to add to list
                VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter();

                int vrcParapLength = vrcParam.parameters.Length;

                //Modify parameter according to user settings and object name
                newParam.name = toggleObjects[i].name + "Toggle";
                newParam.valueType = VRCExpressionParameters.ValueType.Bool;
                newParam.defaultValue = 0;

                //Check to see if parameter is saved
                if (parameterSave == true)
                {
                    newParam.saved = true;
                }
                else
                {
                    newParam.saved = false;
                }

                same = false;

                //THis garbage here checks to see if there is already a parameter with the same name. If so, It ignore it and removes one slip from the predetermined list.
                for (int j = 0; j < vrcParapLength; j++)
                {
                    if (newList[j].name == toggleObjects[i].name + "Toggle")
                    {
                        same = true;
                        newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + toggleObjects.Length - 1 - i];

                        for (int k = 0; k < vrcParam.parameters.Length; k++)
                        {
                            newList[k] = vrcParam.parameters[k];
                        }
                    }
                }

                //If no name name was found, then add parameter to list
                if (same == false)
                {
                    newList[i + vrcParam.parameters.Length] = newParam;
                }
            }
        }
        //Apply new list to VRCExpressionParameter asset
        vrcParam.parameters = newList;


    }

    private void MakeVRCMenu()
    {
        bool menutoggle = false;

        if (!seperateToggles)
        {
            VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control();

            controlItem.name = toggleObjects[0].name + "Group";
            controlItem.type = VRCExpressionsMenu.Control.ControlType.Toggle;
            controlItem.parameter = new VRCExpressionsMenu.Control.Parameter();
            controlItem.parameter.name = toggleObjects[0].name + "ToggleGroup";
            menutoggle = false;
            for (int j = 0; j < vrcMenu.controls.Count; j++)
            {
                if (vrcMenu.controls[j].name == controlItem.parameter.name)
                {
                    menutoggle = true;
                }
            }

            if (menutoggle == false)
            {
                vrcMenu.controls.Add(controlItem);
            }
        }
        else
        {

            for (int i = 0; i < toggleObjects.Length; i++)
            {
                VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control();

                controlItem.name = toggleObjects[i].name;
                controlItem.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                controlItem.parameter = new VRCExpressionsMenu.Control.Parameter();
                controlItem.parameter.name = toggleObjects[i].name + "Toggle";
                menutoggle = false;
                for (int j = 0; j < vrcMenu.controls.Count; j++)
                {
                    if (vrcMenu.controls[j].name == controlItem.parameter.name)
                    {
                        menutoggle = true;
                    }
                }

                if (menutoggle == false)
                {
                    vrcMenu.controls.Add(controlItem);
                }
            }
        }
    }

    private void Postprocessing()
    {
        AssetDatabase.Refresh();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(vrcParam);
        EditorUtility.SetDirty(vrcMenu);

        AssetDatabase.SaveAssets();
    }

    private bool doesNameExistParam(string name, AnimatorControllerParameter[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].name == name)
            {
                return true;
            }
        }
        return false;
    }

    private bool doesNameExistLayer(string name, AnimatorControllerLayer[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].name == name)
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
    void UISetup()
    {
        horizontalLine = new GUIStyle()
        {
            margin = new RectOffset(0, 0, 4, 4),
            fixedHeight = 1
        };
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
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

#endif
