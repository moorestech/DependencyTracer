using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DependencyTracer.Core
{
    /// <summary>
    /// アセット間の依存関係を管理するデータベース
    /// </summary>
    public static class DependencyDatabase
    {
        private const string DatabasePath = "Library/DependencyTracerDB.dat";
        private static DependencyData _data;
        private static bool _isDirty;

        /// <summary>
        /// データベースが初期化済みかどうか
        /// </summary>
        public static bool IsInitialized => _data?.IsInitialized ?? false;

        /// <summary>
        /// 最後に全体解析を実行した日時
        /// </summary>
        public static DateTime LastFullAnalysisTime => _data?.LastFullAnalysisTime ?? DateTime.MinValue;

        /// <summary>
        /// 追跡中のアセット数
        /// </summary>
        public static int TrackedAssetCount => _data?.TrackedAssetCount ?? 0;

        /// <summary>
        /// 全アセットの依存関係を解析
        /// </summary>
        public static void AnalyzeAllAssets()
        {
            var stopwatch = Stopwatch.StartNew();
            var newData = new DependencyData();

            try
            {
                var allAssetPaths = AssetDatabase.GetAllAssetPaths();
                var totalCount = allAssetPaths.Length;
                
                for (var i = 0; i < totalCount; i++)
                {
                    var path = allAssetPaths[i];
                    var progress = (float)i / totalCount;
                    
                    EditorUtility.DisplayProgressBar(
                        "依存関係を解析中...", 
                        $"{path} ({i + 1}/{totalCount})", 
                        progress);

                    newData.TrackAsset(path);
                }

                newData.MarkAsInitialized();
                _data = newData;
                _isDirty = true;
                SaveDatabase();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DependencyTracer] 解析中にエラーが発生しました: {e}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                stopwatch.Stop();
                Debug.Log($"[DependencyTracer] 解析完了 - 時間: {stopwatch.Elapsed:g}, アセット数: {TrackedAssetCount}");
            }
        }

        /// <summary>
        /// 指定されたアセットが依存しているアセットのGUIDリストを取得
        /// </summary>
        public static GUID[] GetDependencies(GUID assetGuid)
        {
            LoadDatabaseIfNeeded();
            return _data?.GetDependencies(assetGuid) ?? Array.Empty<GUID>();
        }

        /// <summary>
        /// 指定されたアセットに依存しているアセットのGUIDリストを取得
        /// </summary>
        public static GUID[] GetReferences(GUID assetGuid)
        {
            LoadDatabaseIfNeeded();
            return _data?.GetReferences(assetGuid) ?? Array.Empty<GUID>();
        }

        /// <summary>
        /// アセットパスから依存関係を取得
        /// </summary>
        public static string[] GetDependenciesByPath(string assetPath)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty()) return Array.Empty<string>();

            var dependencyGuids = GetDependencies(guid);
            var paths = new string[dependencyGuids.Length];
            
            for (var i = 0; i < dependencyGuids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(dependencyGuids[i]);
            }
            
            return paths;
        }

        /// <summary>
        /// アセットパスから参照リストを取得
        /// </summary>
        public static string[] GetReferencesByPath(string assetPath)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty()) return Array.Empty<string>();

            var referenceGuids = GetReferences(guid);
            var paths = new string[referenceGuids.Length];
            
            for (var i = 0; i < referenceGuids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(referenceGuids[i]);
            }
            
            return paths;
        }

        /// <summary>
        /// アセットの変更を通知（AssetPostprocessorから呼ばれる）
        /// </summary>
        internal static void OnAssetsChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets)
        {
            LoadDatabaseIfNeeded();
            if (!IsInitialized) return;

            var hasChanges = false;

            // 削除されたアセットの処理
            foreach (var deletedAsset in deletedAssets)
            {
                hasChanges |= _data.UntrackAsset(deletedAsset);
            }

            // インポート・更新されたアセットの処理
            foreach (var importedAsset in importedAssets)
            {
                hasChanges |= _data.TrackAsset(importedAsset);
            }

            // 移動されたアセットの処理
            foreach (var movedAsset in movedAssets)
            {
                hasChanges |= _data.TrackAsset(movedAsset);
            }

            if (hasChanges)
            {
                _isDirty = true;
                SaveDatabase();
            }
        }

        /// <summary>
        /// データベース情報をコンソールに出力
        /// </summary>
        public static void ShowDatabaseInfo()
        {
            LoadDatabaseIfNeeded();
            
            var info = $@"
=== Dependency Tracer Database Info ===
初期化済み: {IsInitialized}
最終全体解析: {LastFullAnalysisTime:yyyy/MM/dd HH:mm:ss}
追跡アセット数: {TrackedAssetCount:N0}
データベースパス: {DatabasePath}
";
            
            if (File.Exists(DatabasePath))
            {
                var fileInfo = new FileInfo(DatabasePath);
                info += $"ファイルサイズ: {fileInfo.Length / 1024.0 / 1024.0:F2} MB\n";
            }
            
            Debug.Log(info);
        }

        /// <summary>
        /// データベースをクリア
        /// </summary>
        public static void ClearDatabase()
        {
            _data = null;
            _isDirty = false;
            
            if (File.Exists(DatabasePath))
            {
                File.Delete(DatabasePath);
                Debug.Log("[DependencyTracer] データベースをクリアしました");
            }
        }

        private static void LoadDatabaseIfNeeded()
        {
            if (_data != null) return;

            if (!File.Exists(DatabasePath))
            {
                _data = new DependencyData();
                return;
            }

            try
            {
                using (var stream = new FileStream(DatabasePath, FileMode.Open, FileAccess.Read))
                {
                    _data = DependencyData.LoadFrom(stream);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DependencyTracer] データベースの読み込みに失敗しました: {e}");
                _data = new DependencyData();
                
                // 破損したファイルを削除
                try
                {
                    File.Delete(DatabasePath);
                }
                catch
                {
                    // 削除に失敗しても続行
                }
            }
        }

        private static void SaveDatabase()
        {
            if (!_isDirty || _data == null) return;

            try
            {
                var directory = Path.GetDirectoryName(DatabasePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(DatabasePath, FileMode.Create, FileAccess.Write))
                {
                    _data.SaveTo(stream);
                }

                _isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DependencyTracer] データベースの保存に失敗しました: {e}");
            }
        }
    }
}