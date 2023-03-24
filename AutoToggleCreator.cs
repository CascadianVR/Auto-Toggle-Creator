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
        public static Animator myAnimator;
        public static VRCExpressionsMenu vrcMenu;
        public string saveDir;
        public static AnimatorController controller;
        public static VRCExpressionParameters vrcParam;
        public static bool CombineToggles = false;
        
        private Vector2 scrollPos;
        private static GameObject selectedAvatar = null;
        
        // Defines properties of each toggle. Each are added to a list where the user changes the properties before thye're applied.
        public class ToggleType
        {
            public string toggleName;
            public VRCExpressionsMenu expressionMenu;
            public int vrcMenuIndex;

            public int toggleObjectCount;
            public List<GameObject> toggleObject;
            public List<bool> invertObject;

            public int shapekeyNameCount;
            public List<SkinnedMeshRenderer> shapekeyMesh;
            public List<int> shapekeyIndex;
            public List<string> shapekeyName;
            
            public ToggleType() // Assign default values for when created.
            {
                toggleName = "Toggle Name";
                expressionMenu = vrcMenu;
                vrcMenuIndex = 0;

                toggleObject = new List<GameObject>();
                toggleObjectCount = 0;
                invertObject = new List<bool>();

                shapekeyName = new List<string>();
                shapekeyMesh = new List<SkinnedMeshRenderer>();
                shapekeyIndex = new List<int>();
                shapekeyNameCount = 0;
            }
        }

        private void OnValidate()
        {
            Init();
            
        }

        [MenuItem("Cascadian/AutoToggleCreator")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            AutoToggleCreator window = (AutoToggleCreator)EditorWindow.GetWindow(typeof(AutoToggleCreator));
            window.Show();
            window.minSize = new Vector2(450, 650);
            
            // Make sure no data is brought over when a new window is created
            Toggles = new List<ToggleType>();
            myAnimator = null;
            controller = null;
            vrcParam = null;
            vrcMenu = null;
            selectedAvatar = null;
            
            // Default UI Setup for new window
            UISetup();
        }

        private void OnGUI()
        {
            // Creates list of all valid models in the scene for user to choose from.
            AutoFillSelection();
            
            // Disable controls until required assets are assigned
            if (selectedAvatar != null)
                getAvatarInfo(selectedAvatar.GetComponent<VRCAvatarDescriptor>());
            EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true);

            HorizontalLine(Color.white, 5);

            // Manages UI and Logic for grouping toggles and assigning propertis for each.
            ToggleGroupsEditor.GroupList();

            HorizontalLine(Color.white, 10f);
            EditorGUI.EndDisabledGroup();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos ,  GUILayout.ExpandHeight(true), GUILayout.MinHeight(80));
            
            // Checks for any errors the user must address and displays them before continuing with toggle creation.
            bool errorPass = false;
            if (selectedAvatar != null)
            {
                errorPass = debugMenu.CheckErrors();
            }

            EditorGUILayout.EndScrollView();

            HorizontalLine(Color.white, 10f);

            EditorGUI.BeginDisabledGroup((myAnimator && controller && vrcParam && vrcMenu) != true);
            EditorGUI.BeginDisabledGroup(!errorPass); // Disable create button until all errors are addressed
            
            if (GUILayout.Button("Create Toggles!", GUILayout.Height(40f)))
            {
                CreateClips(); // Creates the Animation Clips needed for toggles.
                ApplyToAnimator(); // Handles making properties, layers, states and transitions.
                //ApplyToAnimatorBLEND(); // Handles making properties, layers, states and transitions.
                MakeVRCParameter(); // Makes a new VRCParameter list, populates it with existing parameters, then adds new ones for each toggle.
                MakeVRCMenu(); // Adds the new toggles to the selected VRCMenu with appropriate settings
                Postprocessing(); // Makes sure to save all the newly created and modified assets
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

        }

        private void AutoFillSelection()
        {
            controller = null;
            vrcParam = null;
            vrcMenu = null;
            
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

            if (SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController != null)
            {
                controller = (AnimatorController)SelectedObj.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers[4].animatorController;
            }

            myAnimator = SelectedObj.GetComponent<Animator>();
            vrcParam = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionParameters;
            vrcMenu = SelectedObj.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
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
                    toggleShapeValue = 100f - toggleShapeValue;
                    
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
            if (CombineToggles)
            {
                // Check if the parameter or layer already exists. If not, add parameter
                bool existParam = doesNameExistParam("GroupToggle", controller.parameters);
                if (existParam == false)
                {
                    controller.AddParameter("GroupToggle", AnimatorControllerParameterType.Int);
                }

                //Check if a layer already Exists with that name. If so, remove and add new one.
                int index;
                bool existLayer = doesNameExistLayer("GroupToggle", controller.layers, out index);
                if (existLayer)
                {
                    controller.RemoveLayer(index);
                }

                controller.AddLayer("GroupToggle");

                //Creating Idle, On, and Off states
                AnimatorController animatorController = controller;
                var sm = animatorController.layers[controller.layers.Length - 1].stateMachine;

                sm.AddState("stateIdle", new Vector3(300, 0, 0));

                sm.states[0].state.name = "IDLEGroupToggle";
                sm.states[0].state.motion = (Motion)AssetDatabase.LoadAssetAtPath("Assets/CasTools/VRC-Auto-Toggle-Creator/IDLE.anim", typeof(Motion));
                sm.states[0].state.writeDefaultValues = false;

                int i = 1;
                for (int j = 0; j < Toggles.Count; j++)
                {
                    var toggle = Toggles[j];
                    sm.AddState("stateOn" + j, new Vector3(600, -140 + (j*110), 0));
                    sm.AddState("stateOff" + j, new Vector3(600, -100 + (j*110), 0));
                    
                    sm.states[i].state.name = "ON" + toggle.toggleName;
                    sm.states[i].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion));
                    sm.states[i].state.writeDefaultValues = false;

                    sm.states[i+1].state.name = "OFF" + toggle.toggleName;
                    sm.states[i+1].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion));
                    sm.states[i+1].state.writeDefaultValues = false;

                    sm.states[0].state.AddTransition(sm.states[i].state);
                    sm.states[0].state.transitions[j].AddCondition(AnimatorConditionMode.Equals, j+1, "GroupToggle");
                    sm.states[0].state.transitions[j].hasExitTime = false;
                    sm.states[0].state.transitions[j].duration = 0.01f;
                    sm.states[i].state.AddTransition(sm.states[i+1].state);
                    sm.states[i].state.transitions[0].AddCondition(AnimatorConditionMode.NotEqual, j+1, "GroupToggle");
                    sm.states[i].state.transitions[0].hasExitTime = false;
                    sm.states[i].state.transitions[0].duration = 0.01f;
                    sm.states[i+1].state.AddTransition(sm.states[0].state);
                    sm.states[i+1].state.transitions[0].hasExitTime = true;
                    sm.states[i+1].state.transitions[0].exitTime = 0f;
                    sm.states[i+1].state.transitions[0].duration = 0.01f;
                    Debug.Log(sm.states.Length);

                    i += 2;
                }

                //Set Layer Weight
                AnimatorControllerLayer[] layers = controller.layers;
                layers[controller.layers.Length - 1].defaultWeight = 1;
                controller.layers = layers;
            }
            else
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
                    int index;
                    bool existLayer = doesNameExistLayer(toggle.toggleName, controller.layers, out index);
                    if (existLayer)
                    {
                        controller.RemoveLayer(index);
                    }

                    controller.AddLayer(toggle.toggleName);

                    //Creating Idle, On, and Off states
                    AnimatorController animatorController = controller;
                    var sm = animatorController.layers[controller.layers.Length - 1].stateMachine;

                    sm.AddState("stateIdle", new Vector3(300, 0, 0));
                    sm.AddState("stateOn", new Vector3(300, -220, 0));
                    sm.AddState("stateOff", new Vector3(100, -110, 0));

                    sm.states[0].state.name = "IDLE" + toggle.toggleName;
                    sm.states[0].state.motion = (Motion)AssetDatabase.LoadAssetAtPath("Assets/CasTools/VRC-Auto-Toggle-Creator/IDLE.anim", typeof(Motion));
                    sm.states[0].state.writeDefaultValues = false;
            
                    sm.states[1].state.name = "ON" + toggle.toggleName;
                    sm.states[1].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion));
                    sm.states[1].state.writeDefaultValues = false;
            
                    sm.states[2].state.name = "OFF" + toggle.toggleName;
                    sm.states[2].state.motion = (Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion));
                    sm.states[2].state.writeDefaultValues = false;

                    sm.states[0].state.AddTransition(sm.states[1].state);
                    sm.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.If, 0, toggle.toggleName + "Toggle");
                    sm.states[0].state.transitions[0].hasExitTime = false;
                    sm.states[0].state.transitions[0].duration = 0.01f;
                    sm.states[1].state.AddTransition(sm.states[2].state);
                    sm.states[1].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 0, toggle.toggleName + "Toggle");
                    sm.states[1].state.transitions[0].hasExitTime = false;
                    sm.states[1].state.transitions[0].duration = 0.01f;
                    sm.states[2].state.AddTransition(sm.states[0].state);
                    sm.states[2].state.transitions[0].hasExitTime = true;
                    sm.states[2].state.transitions[0].exitTime = 0f;
                    sm.states[2].state.transitions[0].duration = 0.01f;

                    //Set Layer Weight
                    AnimatorControllerLayer[] layers = controller.layers;
                    layers[controller.layers.Length - 1].defaultWeight = 1;
                    controller.layers = layers;
                }
            }

            AssetDatabase.SaveAssets();
        }

        private void ApplyToAnimatorBLEND()
        {

            //Check if a layer already Exists with that name. If so, remove and add new one.
            int index;
            bool existLayer = doesNameExistLayer(Toggles[0].toggleName, controller.layers, out index);
            if (existLayer)
            {
                controller.RemoveLayer(index);
            }

            controller.AddParameter("Weight", AnimatorControllerParameterType.Float);

            controller.AddLayer("BlendToggles");
            
            AnimatorController animatorController = controller;
            var sm = animatorController.layers[animatorController.layers.Length - 1].stateMachine;
            
            BlendTree MainBlendtree = new BlendTree();
            MainBlendtree.name = "BlendTreeToggle";
            MainBlendtree.hideFlags = HideFlags.HideInHierarchy;
            MainBlendtree.blendType = BlendTreeType.Direct;
            MainBlendtree.blendParameter = "Weight";
            MainBlendtree.blendParameterY = "Weight";
            
            animatorController.parameters[animatorController.parameters.Length - 1].defaultBool = true;
            animatorController.parameters[animatorController.parameters.Length - 1].defaultFloat = 1.0f;
            animatorController.parameters[animatorController.parameters.Length - 1].defaultInt = 1;
            Debug.Log(animatorController.parameters[animatorController.parameters.Length - 1].name);
            
            
            foreach (var toggle in Toggles)
            {

                // Check if the parameter or layer already exists. If not, add parameter
                bool existParam = doesNameExistParam(toggle.toggleName + "Toggle", animatorController.parameters);
                if (existParam == false)
                {
                    animatorController.AddParameter(toggle.toggleName + "Toggle", AnimatorControllerParameterType.Bool);
                }
                
                //Creating On and Off

                BlendTree Blendtree = new BlendTree();
                Blendtree.name = "BlendTreeToggle";

                AssetDatabase.AddObjectToAsset(Blendtree, animatorController);
                Blendtree.hideFlags = HideFlags.HideInHierarchy;
                Blendtree.blendType = BlendTreeType.Simple1D;
                Blendtree.blendParameter = toggle.toggleName + "Toggle";
                
                MainBlendtree.AddChild(Blendtree);

                Blendtree.AddChild((Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/Off{toggle.toggleName}.anim", typeof(Motion)));
                Blendtree.AddChild((Motion)AssetDatabase.LoadAssetAtPath(saveDir + "ToggleAnimations" + $"/On{toggle.toggleName}.anim", typeof(Motion)));
                

                //Set Layer Weight
                AnimatorControllerLayer[] layers = animatorController.layers;
                layers[animatorController.layers.Length - 1].defaultWeight = 1;
                animatorController.layers = layers;
            }

            for (int i = 0; i < MainBlendtree.children.Length; i++)
            {
                MainBlendtree.children[i].directBlendParameter = "Weight";
            }
            
            Debug.Log(MainBlendtree.blendParameter);
            Debug.Log(MainBlendtree.blendParameterY);
            MainBlendtree.children[0].directBlendParameter = "Weight";
            MainBlendtree.children[1].directBlendParameter = "Weight";
            Debug.Log(MainBlendtree.children[0].directBlendParameter);
            Debug.Log(MainBlendtree.children[1].directBlendParameter);
            
            AssetDatabase.SaveAssets();
        }
        
        private void MakeVRCParameter()
        {
            List<VRCExpressionParameters.Parameter> newList = new List<VRCExpressionParameters.Parameter>();

            foreach (var param in vrcParam.parameters)
            {
                newList.Add(param);
            }

            foreach (var toggle in Toggles)
            {
                for (int i = 0; i < newList.Count; i++)
                {
                    if (newList[i].name == toggle.toggleName + "Toggle")
                    {
                        newList.RemoveAt(i);
                    }
                }
                
                //Make new parameter and add to list
                VRCExpressionParameters.Parameter newParam = new VRCExpressionParameters.Parameter
                {
                    name = toggle.toggleName + "Toggle",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = 0
                };
                newList.Add(newParam);
            }
            

            //Apply new list to VRCExpressionParameter asset
            vrcParam.parameters = newList.ToArray();
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

        private bool doesNameExistLayer(string layerName, AnimatorControllerLayer[] array, out int index)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].name == layerName)
                {
                    index = i;
                    return true;
                }
            }
            index = 0;
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
            ToggleGroupsEditor.green = CasHelpers.CreateColorTexture2D(new Color(0, 0.5f, 0));
            ToggleGroupsEditor.darkgreen = CasHelpers.CreateColorTexture2D(new Color(0, 0.3f, 0));
            ToggleGroupsEditor.red = CasHelpers.CreateColorTexture2D(new Color(0.5f, 0, 0));
            ToggleGroupsEditor.darkred = CasHelpers.CreateColorTexture2D(new Color(0.3f, 0, 0));
        }

        private static GUIStyle horizontalLine;
        public static Texture2D plusIcon;
        public static Texture2D minusIcon;

        private static void UISetup()
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
