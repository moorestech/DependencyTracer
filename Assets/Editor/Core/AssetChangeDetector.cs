using UnityEditor;

namespace DependencyTracer.Core
{
    /// <summary>
    /// アセットの変更を検知してデータベースを自動更新
    /// </summary>
    public class AssetChangeDetector : AssetPostprocessor
    {
        /// <summary>
        /// 処理順序（他のPostprocessorより後に実行）
        /// </summary>
        public override int GetPostprocessOrder()
        {
            return int.MaxValue - 100;
        }

        /// <summary>
        /// アセットの変更を検知
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // データベースに変更を通知
            DependencyDatabase.OnAssetsChanged(importedAssets, deletedAssets, movedAssets);
            
            // ビューワーウィンドウが開いている場合は更新を要求
            DependencyViewerWindow.RequestRefresh();
        }
    }
}