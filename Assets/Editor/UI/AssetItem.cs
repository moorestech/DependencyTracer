using System.Collections.Generic;
using System.Linq;
using DependencyTracer.Core;
using DependencyTracer.Utils;
using UnityEditor;
using UnityEngine;

namespace DependencyTracer.UI
{
    /// <summary>
    /// 依存関係ビューワーで表示するアセットアイテム
    /// </summary>
    [System.Serializable]
    public class AssetItem
    {
        [SerializeField]
        private Object _asset;
        
        [SerializeField]
        private string _assetPath;
        
        [SerializeField]
        private GUID _assetGuid;
        
        [SerializeField]
        private bool _dependenciesFoldout = true;
        
        [SerializeField]
        private bool _referencesFoldout = true;
        
        [SerializeField]
        private Dictionary<string, bool> _folderFoldouts = new Dictionary<string, bool>();

        private List<DependencyInfo> _dependencies;
        private List<DependencyInfo> _references;
        private GUIContent _assetContent;

        public Object Asset => _asset;

        public AssetItem(Object asset)
        {
            _asset = asset;
            _assetPath = AssetDatabase.GetAssetPath(asset);
            _assetGuid = AssetDatabase.GUIDFromAssetPath(_assetPath);
            
            UpdateContent();
            Refresh();
        }

        /// <summary>
        /// 依存関係情報を更新
        /// </summary>
        public void Refresh()
        {
            // 依存先を取得
            _dependencies = new List<DependencyInfo>();
            var depGuids = DependencyDatabase.GetDependencies(_assetGuid);
            foreach (var guid in depGuids)
            {
                var info = new DependencyInfo(guid);
                if (info.IsValid)
                {
                    _dependencies.Add(info);
                }
            }

            // 参照元を取得
            _references = new List<DependencyInfo>();
            var refGuids = DependencyDatabase.GetReferences(_assetGuid);
            foreach (var guid in refGuids)
            {
                var info = new DependencyInfo(guid);
                if (info.IsValid)
                {
                    _references.Add(info);
                }
            }
        }

        /// <summary>
        /// GUIを描画（trueを返したら削除）
        /// </summary>
        public bool DrawGUI(bool showDependencies, bool showReferences, bool showIndirectDependencies, bool showHierarchicalView)
        {
            var shouldRemove = false;

            // ヘッダー部分
            using (new EditorGUILayout.HorizontalScope())
            {
                // アセット情報
                if (GUIUtils.DrawAssetButton(_asset, _assetContent.text, GUIUtils.AssetLabelStyle))
                {
                    HandleAssetClick();
                }

                // アセットタイプとサイズ
                GUILayout.Label(AssetUtils.GetAssetTypeName(_asset), EditorStyles.miniLabel, GUILayout.Width(80));
                
                var size = AssetUtils.GetAssetSize(_assetPath);
                if (!string.IsNullOrEmpty(size))
                {
                    GUILayout.Label(size, EditorStyles.miniLabel, GUILayout.Width(60));
                }

                GUILayout.FlexibleSpace();

                // 削除ボタン
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    shouldRemove = true;
                }
            }

            // パス表示
            EditorGUILayout.LabelField(_assetPath, EditorStyles.miniLabel);

            GUIUtils.DrawHorizontalLine();

            // 依存関係表示
            if (showDependencies)
            {
                DrawDependencies(showIndirectDependencies, showHierarchicalView);
            }

            if (showReferences)
            {
                DrawReferences(showIndirectDependencies, showHierarchicalView);
            }

            return shouldRemove;
        }

        private void DrawDependencies(bool showIndirect, bool showHierarchical)
        {
            // 依存先を取得
            var dependencies = GetDependenciesToShow(showIndirect);
            var label = showIndirect ? "依存先 (間接含む)" : "依存先 (直接のみ)";
            label += $" {dependencies.Count}";
            
            using (new EditorGUILayout.HorizontalScope())
            {
                _dependenciesFoldout = EditorGUILayout.Foldout(_dependenciesFoldout, label, true, GUIUtils.FoldoutStyle);
            }

            if (_dependenciesFoldout && dependencies.Count > 0)
            {
                EditorGUI.indentLevel++;
                if (showHierarchical)
                {
                    DrawHierarchicalView(dependencies);
                }
                else
                {
                    DrawListView(dependencies);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawReferences(bool showIndirect, bool showHierarchical)
        {
            // 参照元を取得
            var references = GetReferencesToShow(showIndirect);
            var label = showIndirect ? "参照元 (間接含む)" : "参照元 (直接のみ)";
            label += $" {references.Count}";
            
            using (new EditorGUILayout.HorizontalScope())
            {
                _referencesFoldout = EditorGUILayout.Foldout(_referencesFoldout, label, true, GUIUtils.FoldoutStyle);
            }

            if (_referencesFoldout && references.Count > 0)
            {
                EditorGUI.indentLevel++;
                if (showHierarchical)
                {
                    DrawHierarchicalView(references);
                }
                else
                {
                    DrawListView(references);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void HandleAssetClick()
        {
            if (Event.current.button == 0)
            {
                // 左クリック：選択
                AssetUtils.SelectAndPing(_asset);
            }
            else if (Event.current.button == 1)
            {
                // 右クリック：コンテキストメニュー
                GUIUtils.ShowAssetContextMenu(_asset, _assetPath);
            }
        }

        private void UpdateContent()
        {
            if (_asset != null)
            {
                var icon = AssetUtils.GetAssetIcon(_asset);
                _assetContent = new GUIContent(_asset.name, icon, _assetPath);
            }
            else
            {
                _assetContent = new GUIContent("(Missing Asset)");
            }
        }

        private List<DependencyInfo> GetDependenciesToShow(bool includeIndirect)
        {
            if (!includeIndirect)
            {
                return _dependencies;
            }

            // 間接依存を含む場合
            var allDeps = new List<DependencyInfo>();
            var guids = DependencyDatabase.GetAllDependencies(_assetGuid, true);
            
            foreach (var guid in guids)
            {
                var info = new DependencyInfo(guid);
                if (info.IsValid)
                {
                    allDeps.Add(info);
                }
            }
            
            return allDeps;
        }

        private List<DependencyInfo> GetReferencesToShow(bool includeIndirect)
        {
            if (!includeIndirect)
            {
                return _references;
            }

            // 間接参照を含む場合
            var allRefs = new List<DependencyInfo>();
            var guids = DependencyDatabase.GetAllReferences(_assetGuid, true);
            
            foreach (var guid in guids)
            {
                var info = new DependencyInfo(guid);
                if (info.IsValid)
                {
                    allRefs.Add(info);
                }
            }
            
            return allRefs;
        }

        private void DrawListView(List<DependencyInfo> items)
        {
            // 名前順でソート
            var sortedItems = items.OrderBy(item => item.AssetName).ToList();
            
            foreach (var item in sortedItems)
            {
                item.DrawGUI();
            }
        }

        private void DrawHierarchicalView(List<DependencyInfo> items)
        {
            // ツリー構造を作成
            var rootNode = BuildHierarchyTree(items);
            
            // ルートノードの子要素を描画
            foreach (var child in rootNode.Children.OrderBy(kvp => kvp.Key))
            {
                DrawHierarchyNode(child.Key, child.Value, 0);
            }
        }
        
        private class HierarchyNode
        {
            public Dictionary<string, HierarchyNode> Children = new Dictionary<string, HierarchyNode>();
            public List<DependencyInfo> Items = new List<DependencyInfo>();
        }
        
        private HierarchyNode BuildHierarchyTree(List<DependencyInfo> items)
        {
            var root = new HierarchyNode();
            
            foreach (var item in items)
            {
                var path = item.AssetPath;
                var parts = path.Split('/');
                var current = root;
                
                // ディレクトリ部分を処理
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    if (!current.Children.ContainsKey(part))
                    {
                        current.Children[part] = new HierarchyNode();
                    }
                    current = current.Children[part];
                }
                
                // ファイルをノードに追加
                current.Items.Add(item);
            }
            
            return root;
        }
        
        private void DrawHierarchyNode(string name, HierarchyNode node, int depth)
        {
            var hasChildren = node.Children.Count > 0;
            var hasItems = node.Items.Count > 0;
            var totalCount = node.Items.Count + GetTotalItemCount(node);
            
            name = $"{name} ({totalCount})";
            // フォルダのfoldout状態を管理
            var foldoutKey = $"{depth}_{name}";
            if (!_folderFoldouts.ContainsKey(foldoutKey))
            {
                _folderFoldouts[foldoutKey] = depth < 2; // デフォルトで2階層目まで展開
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 15); // インデント
                
                if (hasChildren || hasItems)
                {
                    // フォルダアイコンとfoldout
                    var foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                    
                    _folderFoldouts[foldoutKey] = EditorGUILayout.Foldout(
                        _folderFoldouts[foldoutKey], 
                        name, 
                        true,
                        foldoutStyle
                    );
                }
                else
                {
                    // 空のフォルダ
                    EditorGUILayout.LabelField(name, EditorStyles.label);
                }
                
                GUILayout.FlexibleSpace();
            }
            
            if (_folderFoldouts[foldoutKey])
            {
                // 子フォルダを表示
                foreach (var child in node.Children.OrderBy(kvp => kvp.Key))
                {
                    DrawHierarchyNode(child.Key, child.Value, depth + 1);
                }
                
                // このフォルダ直下のアイテムを表示
                if (hasItems)
                {
                    foreach (var item in node.Items.OrderBy(i => i.AssetName))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space((depth + 1) * 20); // インデント
                            item.DrawGUI();
                        }
                    }
                }
            }
        }
        
        private int GetTotalItemCount(HierarchyNode node)
        {
            var count = 0;
            foreach (var child in node.Children.Values)
            {
                count += child.Items.Count + GetTotalItemCount(child);
            }
            return count;
        }
    }
}