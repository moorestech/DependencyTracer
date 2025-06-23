using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DependencyTracer.Core
{
    /// <summary>
    /// 依存関係データの保存・読み込みを管理
    /// </summary>
    [Serializable]
    public class DependencyData
    {
        private const int FileVersion = 1;
        private const string FileSignature = "DEPTRACE";

        // アセットGUID -> 依存先GUIDリスト
        private Dictionary<GUID, List<GUID>> _dependencies = new Dictionary<GUID, List<GUID>>();
        
        // アセットGUID -> 参照元GUIDリスト
        private Dictionary<GUID, List<GUID>> _references = new Dictionary<GUID, List<GUID>>();
        
        private DateTime _lastFullAnalysisTime;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public DateTime LastFullAnalysisTime => _lastFullAnalysisTime;
        public int TrackedAssetCount => _dependencies.Count;

        /// <summary>
        /// アセットの依存関係を追跡
        /// </summary>
        public bool TrackAsset(string assetPath)
        {
            // 無効なパスをスキップ
            if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("/"))
                return false;

            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty())
                return false;

            // 現在の依存関係を取得
            var currentDependencies = GetDirectDependencies(assetPath);
            
            // 以前の依存関係と比較
            if (_dependencies.TryGetValue(guid, out var previousDependencies))
            {
                // 変更がない場合はスキップ
                if (AreDependenciesEqual(currentDependencies, previousDependencies))
                    return false;

                // 古い参照関係をクリア
                foreach (var depGuid in previousDependencies)
                {
                    RemoveReference(depGuid, guid);
                }
            }

            // 新しい依存関係を設定
            if (currentDependencies.Count > 0)
            {
                _dependencies[guid] = currentDependencies;
                
                // 参照関係を更新
                foreach (var depGuid in currentDependencies)
                {
                    AddReference(depGuid, guid);
                }
            }
            else
            {
                _dependencies.Remove(guid);
            }

            return true;
        }

        /// <summary>
        /// アセットの追跡を解除
        /// </summary>
        public bool UntrackAsset(string assetPath)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
            if (guid.Empty())
                return false;

            var hasChanges = false;

            // 依存関係を削除
            if (_dependencies.TryGetValue(guid, out var dependencies))
            {
                foreach (var depGuid in dependencies)
                {
                    hasChanges |= RemoveReference(depGuid, guid);
                }
                hasChanges |= _dependencies.Remove(guid);
            }

            // 参照関係を削除
            if (_references.TryGetValue(guid, out var references))
            {
                foreach (var refGuid in references)
                {
                    if (_dependencies.TryGetValue(refGuid, out var refDeps))
                    {
                        hasChanges |= refDeps.Remove(guid);
                        if (refDeps.Count == 0)
                        {
                            _dependencies.Remove(refGuid);
                        }
                    }
                }
                hasChanges |= _references.Remove(guid);
            }

            return hasChanges;
        }

        /// <summary>
        /// 依存先のGUIDリストを取得
        /// </summary>
        public GUID[] GetDependencies(GUID guid)
        {
            return _dependencies.TryGetValue(guid, out var deps) 
                ? deps.ToArray() 
                : Array.Empty<GUID>();
        }

        /// <summary>
        /// 参照元のGUIDリストを取得
        /// </summary>
        public GUID[] GetReferences(GUID guid)
        {
            return _references.TryGetValue(guid, out var refs) 
                ? refs.ToArray() 
                : Array.Empty<GUID>();
        }

        /// <summary>
        /// 初期化済みとしてマーク
        /// </summary>
        public void MarkAsInitialized()
        {
            _isInitialized = true;
            _lastFullAnalysisTime = DateTime.Now;
        }

        /// <summary>
        /// ストリームに保存
        /// </summary>
        public void SaveTo(Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                // ヘッダー情報
                writer.Write(FileSignature);
                writer.Write(FileVersion);
                writer.Write(_isInitialized);
                writer.Write(_lastFullAnalysisTime.Ticks);

                // 依存関係データ
                SaveDictionary(writer, _dependencies);
                SaveDictionary(writer, _references);
            }
        }

        /// <summary>
        /// ストリームから読み込み
        /// </summary>
        public static DependencyData LoadFrom(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // ヘッダー検証
                var signature = reader.ReadString();
                if (signature != FileSignature)
                    throw new InvalidDataException("無効なファイル形式です");

                var version = reader.ReadInt32();
                if (version != FileVersion)
                    throw new InvalidDataException($"サポートされていないバージョンです: {version}");

                var data = new DependencyData
                {
                    _isInitialized = reader.ReadBoolean(),
                    _lastFullAnalysisTime = new DateTime(reader.ReadInt64())
                };

                // データ読み込み
                data._dependencies = LoadDictionary(reader);
                data._references = LoadDictionary(reader);

                return data;
            }
        }

        private List<GUID> GetDirectDependencies(string assetPath)
        {
            var dependencies = AssetDatabase.GetDependencies(assetPath, false);
            var guidList = new List<GUID>();

            foreach (var depPath in dependencies)
            {
                // 自分自身と無効なパスはスキップ
                if (depPath == assetPath || depPath.StartsWith("/"))
                    continue;

                var depGuid = AssetDatabase.GUIDFromAssetPath(depPath);
                if (!depGuid.Empty())
                {
                    guidList.Add(depGuid);
                }
            }

            // GUIDでソートして順序を保証
            guidList.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
            return guidList;
        }

        private bool AreDependenciesEqual(List<GUID> a, List<GUID> b)
        {
            if (a.Count != b.Count) return false;
            
            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            
            return true;
        }

        private void AddReference(GUID targetGuid, GUID sourceGuid)
        {
            if (!_references.TryGetValue(targetGuid, out var refs))
            {
                refs = new List<GUID>();
                _references[targetGuid] = refs;
            }

            if (!refs.Contains(sourceGuid))
            {
                refs.Add(sourceGuid);
                refs.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
            }
        }

        private bool RemoveReference(GUID targetGuid, GUID sourceGuid)
        {
            if (_references.TryGetValue(targetGuid, out var refs))
            {
                var removed = refs.Remove(sourceGuid);
                if (refs.Count == 0)
                {
                    _references.Remove(targetGuid);
                }
                return removed;
            }
            return false;
        }

        private static void SaveDictionary(BinaryWriter writer, Dictionary<GUID, List<GUID>> dict)
        {
            writer.Write(dict.Count);
            
            foreach (var kvp in dict)
            {
                // キーのGUID
                writer.Write(kvp.Key.ToString());

                // 値のGUIDリスト
                writer.Write(kvp.Value.Count);
                foreach (var guid in kvp.Value)
                {
                    writer.Write(guid.ToString());
                }
            }
        }

        private static Dictionary<GUID, List<GUID>> LoadDictionary(BinaryReader reader)
        {
            var dict = new Dictionary<GUID, List<GUID>>();
            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                // キーのGUID
                var keyString = reader.ReadString();
                var key = new GUID(keyString);

                // 値のGUIDリスト
                var valueCount = reader.ReadInt32();
                var values = new List<GUID>(valueCount);
                
                for (var j = 0; j < valueCount; j++)
                {
                    var guidString = reader.ReadString();
                    values.Add(new GUID(guidString));
                }

                dict[key] = values;
            }

            return dict;
        }
    }
}