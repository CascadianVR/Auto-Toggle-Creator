using System.IO;
using UnityEditor;
using UnityEngine;
using System;
//using UnityEditorInternal;
#if UNITY_EDITOR
using static VRC.SDKBase.VRC_AvatarParameterDriver;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Editor;
using UnityEditor.Animations;
public class AutoToggleCreator : EditorWindow
{
    public GameObject[] toggleObjects;
    public Animator myAnimator;
    int ogparamlength;
    AnimatorController controller;
    VRCExpressionParameters vrcParam;
    VRCExpressionsMenu vrcMenu;
    static bool parameterSave;
    static bool defaultOn;

    [MenuItem("Tools/Cascadian/AutoToggleCreator")]

    static void Init()
    {
        // Get existing open window or if none, make a new one:
        AutoToggleCreator window = (AutoToggleCreator)EditorWindow.GetWindow(typeof(AutoToggleCreator));
        window.Show();

    }

    public void OnGUI()
    {
        EditorGUILayout.Space(15);

        if (GUILayout.Button("Auto-Fill with Selected Avatar", GUILayout.Height(30f)))
        {
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

        EditorGUILayout.Space(15);

        EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true);

        EditorGUILayout.BeginHorizontal();
        //Toggle to save VRCParameter values
        parameterSave = (bool)EditorGUILayout.ToggleLeft("Save VRC Parameters?", parameterSave, EditorStyles.boldLabel);

        defaultOn = (bool)EditorGUILayout.ToggleLeft("Start On by Defualt?", defaultOn, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10f);

        //Toggle Object List
        GUILayout.Label("Objects to Toggle On and Off:", EditorStyles.boldLabel);
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty toggleObjectsProperty = so.FindProperty("toggleObjects");
        EditorGUILayout.PropertyField(toggleObjectsProperty, true);
        GUILayout.Space(10f);

        if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
        {
            Preprocessing();
            CreateClips(); //Creates the Animation Clips needed for toggles.
            ApplyToAnimator(); //Handles making toggle bool property, layer setup, states and transitions.
            MakeVRCParameter(); //Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
            MakeVRCMenu();
        }

        EditorGUI.EndDisabledGroup();

        so.ApplyModifiedProperties();
    }

    private void Preprocessing()
    {

    }

    private void CreateClips()
    {
        for (int i = 0; i < toggleObjects.Length; i++)
        {
            //Make animation clips for on and off state and set curves for game objects on and off
            AnimationClip toggleClipOn = new AnimationClip(); //Clip for ON

            float toggleValue = 1;
            if (defaultOn == true) toggleValue = 0f;
            else toggleValue = 1f;

            toggleClipOn.legacy = false;
            toggleClipOn.SetCurve
                (GetGameObjectPath(toggleObjects[i].transform).Substring(myAnimator.gameObject.name.Length+1),
                typeof(GameObject),
                "m_IsActive",
                new AnimationCurve(new Keyframe(0, toggleValue, 0, 0),
                new Keyframe(0.016666668f, toggleValue, 0, 0))
                );

            //Check to see if path exists. If not, create it.
            if (!Directory.Exists($"Assets/ToggleAnimations/{myAnimator.gameObject.name}/"))
            {
                Directory.CreateDirectory($"Assets/ToggleAnimations/{myAnimator.gameObject.name}/");
            }

            //Save on animation clips (Off should not be needed?)
            AssetDatabase.CreateAsset(toggleClipOn, $"Assets/ToggleAnimations/{myAnimator.gameObject.name}/On{toggleObjects[i].name}.anim");
            //AssetDatabase.CreateAsset(toggleClipOff, $"Assets/ToggleAnimations/{myAnimator.gameObject.name}/Off{toggleObjects[i].name}.anim");
            AssetDatabase.SaveAssets();

        }
    }

    private void ApplyToAnimator()
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
                controller.AddLayer(toggleObjects[i].name.Replace(".","_"));

                //Creating On and Off(Empty) states
                AnimatorState stateOn = new AnimatorState();
                stateOn.name = "ON";
                stateOn.motion = (Motion)AssetDatabase.LoadAssetAtPath($"Assets/ToggleAnimations/{myAnimator.gameObject.name}/On{toggleObjects[i].name}.anim", typeof(Motion));
                AnimatorState stateOff = new AnimatorState();
                stateOff.name = "OFF";
                stateOff.motion = (Motion)AssetDatabase.LoadAssetAtPath($"Assets/ToggleAnimations/{myAnimator.gameObject.name}/Off{toggleObjects[i].name}.anim", typeof(Motion));

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
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[0].duration = 0.01f;

                //If False, go to Off (Empty) state.
                controller.layers[controller.layers.Length - 1].stateMachine.AddAnyStateTransition(OnOff.destinationState);
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].AddCondition(AnimatorConditionMode.If, 0,
                    toggleObjects[i].name + "Toggle");
                controller.layers[controller.layers.Length - 1].stateMachine.anyStateTransitions[1].duration = 0.01f;
            }

            //Set Layer Weight
            UnityEditor.Animations.AnimatorControllerLayer[] layers = controller.layers;
            layers[controller.layers.Length - 1].defaultWeight = 1;
            controller.layers = layers;


        }
    }

    private void MakeVRCParameter()
    {
        VRCExpressionParameters.Parameter[] newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + toggleObjects.Length];

        ogparamlength = vrcParam.parameters.Length;

        //Add parameters that were already on the SO
        for (int i = 0; i < vrcParam.parameters.Length; i++)
        {
            newList[i] = vrcParam.parameters[i];
        }
        bool same = false;
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
            if (parameterSave == true) { newParam.saved = true; } else { newParam.saved = false; }

            same = false;

            //THis garbage here checks to see if there is already a parameter with the same name. If so, It ignore it and removes one slip from the predetermined list.
            for (int j = 0; j < vrcParapLength; j++)
            {
                if (newList[j].name == toggleObjects[i].name + "Toggle")
                {
                    same = true;
                    newList = new VRCExpressionParameters.Parameter[vrcParam.parameters.Length + toggleObjects.Length - 1 -i];

                    for (int k = 0; k < vrcParam.parameters.Length; k++)
                    {
                        newList[k] = vrcParam.parameters[k];
                    }
                }
            }

            //If no name name was found, then add parameter to list
            if (same == false) {
                newList[i + vrcParam.parameters.Length] = newParam;
            }
        }
        //Apply new list to VRCExpressionParameter asset
        vrcParam.parameters = newList;
    }

    private void MakeVRCMenu()
    {
        bool menutoggle = false;
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


}
#endif
