using System.Collections.Generic;
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
        public bool DrawGUI(bool showDependencies, bool showReferences)
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
                DrawDependencies();
            }

            if (showReferences)
            {
                DrawReferences();
            }

            return shouldRemove;
        }

        private void DrawDependencies()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _dependenciesFoldout = EditorGUILayout.Foldout(_dependenciesFoldout, "依存先", true, GUIUtils.FoldoutStyle);
                GUIUtils.DrawCountBadge(_dependencies.Count, new Color(0.3f, 0.6f, 1f));
                GUILayout.FlexibleSpace();
            }

            if (_dependenciesFoldout && _dependencies.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var dep in _dependencies)
                {
                    dep.DrawGUI();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawReferences()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _referencesFoldout = EditorGUILayout.Foldout(_referencesFoldout, "参照元", true, GUIUtils.FoldoutStyle);
                GUIUtils.DrawCountBadge(_references.Count, new Color(1f, 0.6f, 0.3f));
                GUILayout.FlexibleSpace();
            }

            if (_referencesFoldout && _references.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var reference in _references)
                {
                    reference.DrawGUI();
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
    }
}