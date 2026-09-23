using UnityEditor;
using UnityEngine;

namespace GameMatch3.Gameplay.Board.Editor
{
    // Bổ sung nút thao tác preview và giải thích cấu hình ngay trong Inspector của Unity.
    [CustomEditor(typeof(BoardController))]
    public sealed class BoardControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Giữ toàn bộ trường cấu hình mặc định của BoardController.
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Board hỗ trợ từ 5 x 5 đến 12 x 12. Cell Size mặc định là 1. " +
                "Camera tự căn để luôn nhìn trọn board và chừa phần bên trái cho HUD tương lai. " +
                "Preview hiển thị ngay trong Scene. " +
                "Tile Pool luôn có 10 Type ID; bật loại dùng cho level, chỉnh Spawn Weight, " +
                "màu prototype và sprite 2D tại đây. Một level cần bật ít nhất 2 Type ID. " +
                "Bàn đầu có ít nhất 2 nước hợp lệ; dead-board sẽ tự xáo mà không tạo object mới. " +
                "Special Object tạm dùng elip (Match-4/2x2), ngũ giác (Wrapped) và lục giác (Color Bomb). " +
                "Level, Board, Object Pool, Target và Moves được cấu hình trong Window > Game Match 3 > Level Editor. " +
                "Góc trái dưới được để trống cho booster tương lai. " +
                "Các tile con được tạo tự động và không lưu vào scene.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                // Không cho dựng lại preview lúc Play vì board đang chạy gameplay.
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
