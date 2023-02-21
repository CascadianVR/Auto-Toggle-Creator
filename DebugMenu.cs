#if UNITY_EDITOR
using System;
using System.Collections.Generic;


using UnityEditor;

namespace CasTools.VRC_Auto_Toggle_Creator
{

    public class DebugMenu
    {
        int Errors;
        
        public bool CheckErrors()
        {
            Errors = 0;
            
            CheckDescriptorAssignment();
            CheckToggleNames();
            CheckMenuAssignment();

            return Errors == 0;
        }

        private void CheckDescriptorAssignment()
        {

            if (AutoToggleCreator.controller == null)
            {
                EditorGUILayout.HelpBox("Error: No FX Controller assigned to Avatar Descriptor.", MessageType.Error);
                Errors++;
            }
            if (AutoToggleCreator.vrcMenu == null)
            {
                EditorGUILayout.HelpBox("Error: No Expresison Menu assigned to Avatar Descriptor.", MessageType.Error);
                Errors++;
            }
            if (AutoToggleCreator.vrcParam == null)
            {
                EditorGUILayout.HelpBox("Error: No Expresison Parameters assigned to Avatar Descriptor.", MessageType.Error);
                Errors++;
            }

        }
        
        private void CheckToggleNames()
        {
            string[] names = new string[AutoToggleCreator.Toggles.Count];

            for (int i = 0; i < AutoToggleCreator.Toggles.Count; i++)
            {
                names[i] = AutoToggleCreator.Toggles[i].toggleName;
            }

            foreach (var n in names)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (Array.IndexOf(names, n) == i || n != names[i]) continue;
                    EditorGUILayout.HelpBox("Error: Multiple toggle groups share the same name. Please rename to resolve.", MessageType.Error);
                    Errors++;
                    return;
                }
            }
        }

        private void CheckMenuAssignment()
        {
            foreach (var t in AutoToggleCreator.Toggles)
            {
                if (t.expressionMenu != null) continue;
                EditorGUILayout.HelpBox("Error: Missing Expression Menu for \"" + t.toggleName + "\"", MessageType.Error);
                Errors++;
            }
        }
    }
}
#endif