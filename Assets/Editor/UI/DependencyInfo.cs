using DependencyTracer.Core;
using DependencyTracer.Utils;
using UnityEditor;
using UnityEngine;

namespace DependencyTracer.UI
{
    /// <summary>
    /// 個別の依存関係情報
    /// </summary>
    public class DependencyInfo
    {
        private readonly GUID _guid;
        private readonly string _assetPath;
        private readonly Object _asset;
        private readonly GUIContent _content;
        
        public bool IsValid => _asset != null;

        public DependencyInfo(GUID guid)
        {
            _guid = guid;
            _assetPath = AssetDatabase.GUIDToAssetPath(guid);
            
            if (!string.IsNullOrEmpty(_assetPath))
            {
                _asset = AssetDatabase.LoadAssetAtPath<Object>(_assetPath);
                if (_asset != null)
                {
                    var icon = AssetUtils.GetAssetIcon(_asset);
                    _content = new GUIContent(_asset.name, icon, _assetPath);
                }
                else
                {
                    _content = new GUIContent($"(Missing: {_assetPath})");
                }
            }
            else
            {
                _content = new GUIContent($"(Unknown GUID: {guid})");
            }
        }

        /// <summary>
        /// GUI描画
        /// </summary>
        public void DrawGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var clicked = false;
                
                // アイコンを小さく表示
                if (_asset != null)
                {
                    var icon = AssetUtils.GetAssetIcon(_asset);
                    if (icon != null)
                    {
                        var iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    
                    // アセット名ボタン（アイコンなし）
                    clicked = GUILayout.Button(_asset.name, GUIUtils.SmallAssetLabelStyle);
                }
                else
                {
                    // Missing/Unknownアセットの場合
                    GUILayout.Space(20); // アイコン分のスペース
                    clicked = GUILayout.Button(_content.text, GUIUtils.SmallAssetLabelStyle);
                }
                
                // タイプラベル
                if (_asset != null)
                {
                    GUILayout.Label(AssetUtils.GetAssetTypeName(_asset), EditorStyles.miniLabel, GUILayout.Width(80));
                }
                
                GUILayout.FlexibleSpace();

                // 依存関係を新しいアイテムとして開くボタン
                if (_asset != null && GUILayout.Button("→", GUILayout.Width(25)))
                {
                    var window = DependencyViewerWindow.ShowWindow() as DependencyViewerWindow;
                    window?.AddAssets(new[] { _asset });
                }

                if (clicked)
                {
                    HandleClick();
                }
            }
        }

        private void HandleClick()
        {
            if (Event.current.button == 0)
            {
                // 左クリック：選択
                if (_asset != null)
                {
                    AssetUtils.SelectAndPing(_asset);
                }
            }
            else if (Event.current.button == 1)
            {
                // 右クリック：コンテキストメニュー
                if (_asset != null)
                {
                    GUIUtils.ShowAssetContextMenu(_asset, _assetPath);
                }
            }
        }
    }
}