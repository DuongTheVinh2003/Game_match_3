using UnityEditor;
using UnityEngine;

namespace GameMatch3.Gameplay.Board.Editor
{
    [CustomEditor(typeof(BoardController))]
    public sealed class BoardControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Board hỗ trợ từ 6 x 6 đến 8 x 8. Cell Size mặc định là 1. " +
                "Camera tự căn để luôn nhìn trọn board và chừa phần bên trái cho HUD tương lai. " +
                "Preview hiển thị ngay trong Scene. " +
                "Các tile con được tạo tự động và không lưu vào scene.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Rebuild Board Preview"))
                {
                    ((BoardController)target).RebuildPreview();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Randomize Board Preview"))
                {
                    ((BoardController)target).RandomizePreview();
                    EditorUtility.SetDirty(target);
                    SceneView.RepaintAll();
                }
            }
        }
    }
}
