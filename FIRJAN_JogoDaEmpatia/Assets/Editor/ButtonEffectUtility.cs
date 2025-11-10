using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using FIRJAN.UI;

namespace FIRJAN.Editor
{
    /// <summary>
    /// Utilitário para adicionar ButtonClickEffect em todos os botões do projeto
    /// </summary>
    public class ButtonEffectUtility : EditorWindow
    {
        private int _buttonsFound = 0;
        private int _buttonsWithEffect = 0;
        private int _buttonsWithoutEffect = 0;

        [MenuItem("Tools/FIRJAN/Add Button Effects to All Buttons")]
        public static void ShowWindow()
        {
            GetWindow<ButtonEffectUtility>("Button Effects Utility");
        }

        private void OnGUI()
        {
            GUILayout.Label("Button Click Effect Utility", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Esta ferramenta adiciona o componente ButtonClickEffect em todos os botões da cena atual.\n\n" +
                "O ButtonClickEffect adiciona:\n" +
                "• Efeito de punch ao clicar\n" +
                "• Cursor de mãozinha ao passar o mouse\n" +
                "• Efeito de hover (leve aumento)",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
            {
                ScanCurrentScene();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Add Effects to All Buttons in Scene", GUILayout.Height(30)))
            {
                AddEffectsToAllButtons();
            }

            GUILayout.Space(10);

            if (_buttonsFound > 0)
            {
                GUILayout.Label("Scan Results:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Total Buttons Found:", _buttonsFound.ToString());
                EditorGUILayout.LabelField("With Effect:", _buttonsWithEffect.ToString(), new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
                EditorGUILayout.LabelField("Without Effect:", _buttonsWithoutEffect.ToString(), new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } });
            }
        }

        private void ScanCurrentScene()
        {
            _buttonsFound = 0;
            _buttonsWithEffect = 0;
            _buttonsWithoutEffect = 0;

            Button[] buttons = FindObjectsOfType<Button>(true); // incluir inativos
            _buttonsFound = buttons.Length;

            foreach (Button button in buttons)
            {
                if (button.GetComponent<ButtonClickEffect>() != null)
                {
                    _buttonsWithEffect++;
                }
                else
                {
                    _buttonsWithoutEffect++;
                }
            }


        }

        private void AddEffectsToAllButtons()
        {
            Button[] buttons = FindObjectsOfType<Button>(true); // incluir inativos
            int added = 0;

            foreach (Button button in buttons)
            {
                if (button.GetComponent<ButtonClickEffect>() == null)
                {
                    Undo.AddComponent<ButtonClickEffect>(button.gameObject);
                    added++;
                }
            }

            // Marcar a cena como modificada
            if (added > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                // Debug.Log($"[ButtonEffectUtility] ✅ Added ButtonClickEffect to {added} buttons!");
            }
            else
            {
                // Debug.Log($"[ButtonEffectUtility] ℹ️ All buttons already have ButtonClickEffect");
            }

            // Atualizar scan
            ScanCurrentScene();
        }
    }

    /// <summary>
    /// Menu de contexto para adicionar/remover ButtonClickEffect de um botão específico
    /// </summary>
    [CustomEditor(typeof(Button))]
    public class ButtonEditor : UnityEditor.UI.ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Button button = (Button)target;
            ButtonClickEffect effect = button.GetComponent<ButtonClickEffect>();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Click Effect", EditorStyles.boldLabel);

            if (effect == null)
            {
                EditorGUILayout.HelpBox("This button doesn't have click effects", MessageType.Info);
                if (GUILayout.Button("Add Click Effect"))
                {
                    Undo.AddComponent<ButtonClickEffect>(button.gameObject);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This button has click effects! ✨", MessageType.None);
                if (GUILayout.Button("Remove Click Effect"))
                {
                    Undo.DestroyObjectImmediate(effect);
                }
            }
        }
    }
}
