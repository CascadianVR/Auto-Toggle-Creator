#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public class AutoToggleCreator : EditorWindow
    {
        private readonly DebugMenu debugMenu = new DebugMenu();

        public static List<ToggleType> Toggles = new List<ToggleType>();
        public static int vrcMenuIndex;
        public static Animator myAnimator;
        private static AnimatorController controller;
        private static VRCExpressionParameters vrcParam;
        public static VRCExpressionsMenu vrcMenu;
        public string saveDir;
        
        private Vector2 scrollPos2;
        private GameObject selectedAvatar = null;
        
        public class ToggleType
        {
            public string toggleName;
            public VRCExpressionsMenu expressionMenu;

            public int toggleObjectCount;
            public List<GameObject> toggleObject;
            public List<bool> invertObject;
            public bool groupObject;

            public int shapekeyNameCount;
            public List<SkinnedMeshRenderer> shapekeyMesh;
            public List<int> shapekeyIndex;
            public List<string> shapekeyName;
            public List<float> shapekeyValue;
            
            public ToggleType()
            {
                toggleName = "Toggle Name";
                expressionMenu = vrcMenu;

                toggleObject = new List<GameObject>();
                toggleObjectCount = 0;
                invertObject = new List<bool>();
                groupObject = false;

                shapekeyName = new List<string>();
                shapekeyValue = new List<float>();
                shapekeyMesh = new List<SkinnedMeshRenderer>();
                shapekeyIndex = new List<int>();
                shapekeyNameCount = 0;
            }
        }

        [MenuItem("Cascadian/AutoToggleCreator")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            AutoToggleCreator window = (AutoToggleCreator)EditorWindow.GetWindow(typeof(AutoToggleCreator));
            window.Show();
            window.minSize = new Vector2(450, 650);
            Toggles = new List<ToggleType>();
            myAnimator = null;
            controller = null;
            vrcParam = null;
            vrcMenu = null;
        }

        private void OnGUI()
        {
            UISetup();

            AutoFillSelection();
            EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true); // Disable controls until required assets are assigned

            HorizontalLine(Color.white, 5);

            ToggleGroupsEditor.GroupList(); // Manages UI and Logic for grouping toggles and assigning propertis for each.

            HorizontalLine(Color.white, 10f);

            scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2 ,  GUILayout.ExpandHeight(true), GUILayout.MinHeight(80));
            bool errorPass = debugMenu.CheckErrors();
            EditorGUILayout.EndScrollView();

            HorizontalLine(Color.white, 10f);
            
            EditorGUI.BeginDisabledGroup(!errorPass); // Disable controls until required assets are assigned
            
            if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
            {
                CreateClips(); // Creates the Animation Clips needed for toggles.
                ApplyToAnimator(); // Handles making toggle bool property, layer setup, states and transitions.
                MakeVRCParameter(); // Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
                MakeVRCMenu(); // Adds the new toggles to the selected VRCMenu with appropriate settings
                Postprocessing(); // Makes sure to save all the newly created and modified assets
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

        }

        private void AutoFillSelection()
        {
            var style = new GUIStyle()
            {
                padding = new RectOffset(10,10,6,6),
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
                if (selectedAvatar != null && (avatar.gameObject.name == selectedAvatar.name)) buttonStyle.normal.textColor = Color.cyan;
                else buttonStyle.normal.textColor = Color.white;
                if(GUILayout.Button(avatar.name, buttonStyle)) getAvatarInfo(avatar);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndVertical();
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

        private void CreateClips()
        {
            foreach (var toggle in Toggles)
            {
                AnimationClip toggleClipOn = new AnimationClip() { legacy = false }; //Clip for ON
                AnimationClip toggleClipOff = new AnimationClip() { legacy = false }; //Clip for OFF

                for (int i = 0; i < toggle.toggleObject.Count; i++)
                {
                    bool isActive = toggle.toggleObject[i].activeSelf;

                    float toggleObjValue = !isActive ? 1f : 0f;
                    if (toggle.invertObject[i]) toggleObjValue = 1f - toggleObjValue;

                    toggleClipOn.SetCurve(
                        GetGameObjectPath(toggle.toggleObject[i].transform).Substring(GetGameObjectPath(myAnimator.transform).Length + 1),
                        typeof(GameObject),
                        "m_IsActive",
                        new AnimationCurve(new Keyframe(0, toggleObjValue, 0, 0),
                            new Keyframe(0.016666668f, toggleObjValue, 0, 0))
                    );
                    toggleClipOff.SetCurve(
                        GetGameObjectPath(toggle.toggleObject[i].transform).Substring(GetGameObjectPath(myAnimator.transform).Length + 1),
                        typeof(GameObject),
                        "m_IsActive",
                        new AnimationCurve(new Keyframe(0, 1f - toggleObjValue, 0, 0),
                            new Keyframe(0.016666668f, 1f - toggleObjValue, 0, 0))
                    );
                }

                for (int i = 0; i < toggle.shapekeyName.Count; i++) // For each shapekey add 
                {
                    float toggleShapeValue = toggle.shapekeyMesh[i].GetBlendShapeWeight(toggle.shapekeyMesh[i].sharedMesh.GetBlendShapeIndex(toggle.shapekeyName[i]));
                    
                    toggleClipOn.SetCurve(
                        GetGameObjectPath(toggle.shapekeyMesh[i].transform).Substring(GetGameObjectPath(myAnimator.transform).Length + 1),
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + toggle.shapekeyName[i],
                        new AnimationCurve(new Keyframe(0, toggleShapeValue, 0, 0),
                            new Keyframe(0.016666668f, toggleShapeValue, 0, 0))
                    );
                    toggleClipOff.SetCurve(
                        GetGameObjectPath(toggle.shapekeyMesh[i].transform).Substring(GetGameObjectPath(myAnimator.transform).Length + 1),
                        typeof(SkinnedMeshRenderer),
                        "blendShape." + toggle.shapekeyName[i],
                        new AnimationCurve(new Keyframe(0, 100f - toggleShapeValue, 0, 0),
                            new Keyframe(0.016666668f, 100f - toggleShapeValue, 0, 0))
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
                if (toggle.groupObject)
                {

                    // Check if the parameter or layer already exists. If not, add parameter
                    bool existParam = doesNameExistParam("GroupToggle", controller.parameters);
                    if (existParam == false)
                    {
                        controller.AddParameter("GroupToggle", AnimatorControllerParameterType.Int);
                    }

                    //Check if a layer already Exists with that name. If so, remove and add new one.
                    bool existLayer = doesNameExistLayer("GroupToggle", controller.layers);
                    if (existLayer)
                    {
                        controller.RemoveLayer(controller.layers.Length - 1);
                    }

                    controller.AddLayer("GroupToggle");

                    //Creating On and Off states
                    AnimatorState stateOn = new AnimatorState
                    {
                        name = "ON",
                        motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion)),
                        writeDefaultValues = false
                    };
                    AnimatorState stateOff = new AnimatorState
                    {
                        name = "OFF",
                        motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion)),
                        writeDefaultValues = false
                    };

                    AnimatorState stateIdle = new AnimatorState
                    {
                        name = "Idle",
                        motion = (Motion)AssetDatabase.LoadAssetAtPath(Application.dataPath + "/CasTools/VRC-Auto-Toggle-Creator/IDLE.anim", typeof(Motion)),
                        writeDefaultValues = true
                    };

                    ChildAnimatorState[] stateCheck = controller.layers[controller.layers.Length - 1].stateMachine.states;
                    foreach (var t in stateCheck)
                    {
                        if (t.state.name == "Idle")
                        {
                            controller.layers[controller.layers.Length - 1].stateMachine.AddState(t.state, new Vector3(300, 0, 0));
                            break;
                        }

                        controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateIdle, new Vector3(300, 0, 0));
                        break;
                    }

                    // Adding created states to controller layer
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOff, new Vector3(100, -110, 0));
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOn, new Vector3(300, -220, 0));

                    // If True, go to ON state.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[1].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 0,
                        toggle.toggleName + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.transitions[0].duration = 0.1f;

                    // If False, go to Off state.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[2].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.If, 0,
                        toggle.toggleName + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.transitions[0].duration = 0.1f;

                    // Go to Idle after Off state immediately.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[0].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.transitions[0].duration = 0.1f;
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.transitions[0].hasExitTime = true;

                    AssetDatabase.AddObjectToAsset(stateOn, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.AddObjectToAsset(stateOff, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.AddObjectToAsset(stateIdle, AssetDatabase.GetAssetPath(controller));

                    //Set Layer Weight
                    AnimatorControllerLayer[] layers = controller.layers;
                    layers[controller.layers.Length - 1].defaultWeight = 1;
                    controller.layers = layers;
                }
                else
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
                        controller.RemoveLayer(controller.layers.Length - 1);
                    }

                    controller.AddLayer(toggle.toggleName);

                    //Creating On and Off states
                    AnimatorState stateOn = new AnimatorState
                    {
                        name = "ON",
                        motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion)),
                        writeDefaultValues = false
                    };
                    AnimatorState stateOff = new AnimatorState
                    {
                        name = "OFF",
                        motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion)),
                        writeDefaultValues = false
                    };
                    AnimatorState stateIdle = new AnimatorState
                    {
                        name = "Idle",
                        motion = null,
                        writeDefaultValues = false
                    };

                    // Adding created states to controller layer
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateIdle, new Vector3(300, 0, 0));
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOff, new Vector3(100, -110, 0));
                    controller.layers[controller.layers.Length - 1].stateMachine.AddState(stateOn, new Vector3(300, -220, 0));

                    // If True, go to ON state.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[1].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 0,
                        toggle.toggleName + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.states[2].state.transitions[0].duration = 0.1f;

                    // If False, go to Off state.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[2].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.If, 0,
                        toggle.toggleName + "Toggle");
                    controller.layers[controller.layers.Length - 1].stateMachine.states[0].state.transitions[0].duration = 0.1f;

                    // Go to Idle after Off state immediately.
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.AddTransition(
                        controller.layers[controller.layers.Length - 1].stateMachine.states[0].state);
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.transitions[0].duration = 0.1f;
                    controller.layers[controller.layers.Length - 1].stateMachine.states[1].state.transitions[0].hasExitTime = true;

                    //Set Layer Weight
                    AnimatorControllerLayer[] layers = controller.layers;
                    layers[controller.layers.Length - 1].defaultWeight = 1;
                    controller.layers = layers;
                    
                    
                    EditorUtility.SetDirty(stateOn);
                    EditorUtility.SetDirty(stateOff);
                    EditorUtility.SetDirty(stateIdle);
                    EditorUtility.SetDirty(controller);

                    AssetDatabase.AddObjectToAsset(stateOn, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.AddObjectToAsset(stateOff, AssetDatabase.GetAssetPath(controller));
                    AssetDatabase.AddObjectToAsset(stateIdle, AssetDatabase.GetAssetPath(controller));
                    
                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.SaveAssets();
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
                VRCExpressionsMenu.Control controlItem = new VRCExpressionsMenu.Control
                {
                    name = toggle.toggleName,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter { name = toggle.toggleName + "Toggle" }
                };

                for (int i = 0; i < toggle.expressionMenu.controls.Count; i++)
                {
                    if (toggle.expressionMenu.controls[i].name == controlItem.name)
                    {
                        toggle.expressionMenu.controls.RemoveAt(i);
                    }
                }

                toggle.expressionMenu.controls.Add(controlItem);
            }
        }

        private void Postprocessing()
        {
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(vrcParam);
            EditorUtility.SetDirty(vrcMenu);
            
            AssetDatabase.Refresh();

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

        private void OnEnable()
        {
            ToggleGroupsEditor.green = Helpers.CreateColorTexture2D(new Color(0, 0.5f, 0));
            ToggleGroupsEditor.darkgreen = Helpers.CreateColorTexture2D(new Color(0, 0.3f, 0));
            ToggleGroupsEditor.red = Helpers.CreateColorTexture2D(new Color(0.5f, 0, 0));
            ToggleGroupsEditor.darkred = Helpers.CreateColorTexture2D(new Color(0.3f, 0, 0));
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        private void OnAfterAssemblyReload()
        {
            Debug.Log("After Assembly Reload");
        }

        private GUIStyle horizontalLine;
        public static Texture2D plusIcon;
        public static Texture2D minusIcon;

        private void UISetup()
        {
            horizontalLine = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 4, 4),
                fixedHeight = 1
            };
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;

            if (plusIcon == null || minusIcon == null)
            {
                plusIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/CasTools/VRC-Auto-Toggle-Creator/plus.png", typeof(Texture2D));
                minusIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/CasTools/VRC-Auto-Toggle-Creator/minus.png", typeof(Texture2D));
            }
        }

        private void HorizontalLine(Color color, float spacing)
        {
            GUILayout.Space(spacing);
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
            GUILayout.Space(spacing);
        }

    }
}
#endif
