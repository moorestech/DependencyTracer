using UnityEditor;
using UnityEngine;

namespace DependencyTracer.Utils
{
    /// <summary>
    /// アセット関連のユーティリティ
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        /// アセットのアイコンを取得
        /// </summary>
        public static Texture GetAssetIcon(Object asset)
        {
            if (asset == null) return null;
            
            var content = EditorGUIUtility.ObjectContent(asset, asset.GetType());
            return content?.image;
        }

        /// <summary>
        /// アセットのタイプ名を取得
        /// </summary>
        public static string GetAssetTypeName(Object asset)
        {
            if (asset == null) return "Unknown";
            
            var type = asset.GetType();
            
            // よく使われるタイプは分かりやすい名前に変換
            if (type == typeof(GameObject)) return "Prefab";
            if (type == typeof(SceneAsset)) return "Scene";
            if (type == typeof(Material)) return "Material";
            if (type == typeof(Texture2D)) return "Texture";
            if (type == typeof(AudioClip)) return "Audio";
            if (type == typeof(AnimationClip)) return "Animation";
            if (type == typeof(MonoScript)) return "Script";
            if (type == typeof(ScriptableObject)) return "ScriptableObject";
            if (type == typeof(Shader)) return "Shader";
            if (type == typeof(ComputeShader)) return "Compute Shader";
            if (type == typeof(Font)) return "Font";
            
            return type.Name;
        }

        /// <summary>
        /// アセットのサイズを取得（フォーマット済み文字列）
        /// </summary>
        public static string GetAssetSize(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "";
            
            var fullPath = System.IO.Path.Combine(Application.dataPath, "..", assetPath);
            if (!System.IO.File.Exists(fullPath)) return "";
            
            try
            {
                var fileInfo = new System.IO.FileInfo(fullPath);
                return FormatFileSize(fileInfo.Length);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// ファイルサイズを読みやすい形式にフォーマット
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// アセットをプロジェクトビューで選択・ハイライト
        /// </summary>
        public static void SelectAndPing(Object asset)
        {
            if (asset == null) return;
            
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>
        /// アセットのフルパスを取得
        /// </summary>
        public static string GetFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return "";
            return System.IO.Path.Combine(Application.dataPath, "..", assetPath);
        }

        /// <summary>
        /// パスをクリップボードにコピー
        /// </summary>
        public static void CopyPathToClipboard(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                GUIUtility.systemCopyBuffer = path;
                Debug.Log($"Copied to clipboard: {path}");
            }
        }
    }
}