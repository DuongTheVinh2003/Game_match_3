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
                "Tile Pool có đủ 12 con giáp Việt Nam; level chọn tập tile hoạt động và Spawn Weight " +
                "trong Level Editor. Số tile được chọn chính là Pool Size và một level cần ít nhất 2 loại. " +
                "Idle, blink, selected và destroy có thể tinh chỉnh trong Zodiac Tile Animation. " +
                "Bàn đầu có ít nhất 2 nước hợp lệ; dead-board sẽ tự xáo mà không tạo object mới. " +
                "Special dùng mũ quan + hào quang cho Match-4, pháo hoa cho T/L và giáp cho Match-5. " +
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
