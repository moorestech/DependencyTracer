using UnityEditor;
using UnityEngine;

namespace DependencyTracer.Utils
{
    /// <summary>
    /// GUI描画関連のユーティリティ
    /// </summary>
    public static class GUIUtils
    {
        private static GUIStyle _foldoutStyle;
        private static GUIStyle _assetLabelStyle;
        private static GUIStyle _countBadgeStyle;
        private static GUIStyle _smallAssetLabelStyle;

        /// <summary>
        /// カスタムFoldoutスタイルを取得
        /// </summary>
        public static GUIStyle FoldoutStyle
        {
            get
            {
                if (_foldoutStyle == null)
                {
                    _foldoutStyle = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 12,
                        margin = new RectOffset(0, 0, 2, 2)
                    };
                }
                return _foldoutStyle;
            }
        }

        /// <summary>
        /// アセットラベル用スタイルを取得
        /// </summary>
        public static GUIStyle AssetLabelStyle
        {
            get
            {
                if (_assetLabelStyle == null)
                {
                    _assetLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        padding = new RectOffset(2, 2, 2, 2),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _assetLabelStyle;
            }
        }

        /// <summary>
        /// カウントバッジ用スタイルを取得
        /// </summary>
        public static GUIStyle CountBadgeStyle
        {
            get
            {
                if (_countBadgeStyle == null)
                {
                    _countBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                        fontSize = 10,
                        fontStyle = FontStyle.Bold,
                        padding = new RectOffset(4, 4, 1, 1),
                        margin = new RectOffset(4, 4, 0, 0)
                    };
                }
                return _countBadgeStyle;
            }
        }

        /// <summary>
        /// 小さいアセットラベル用スタイルを取得
        /// </summary>
        public static GUIStyle SmallAssetLabelStyle
        {
            get
            {
                if (_smallAssetLabelStyle == null)
                {
                    _smallAssetLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        padding = new RectOffset(2, 2, 2, 2),
                        margin = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 16,
                        imagePosition = ImagePosition.ImageLeft
                    };
                }
                return _smallAssetLabelStyle;
            }
        }

        /// <summary>
        /// 横線を描画
        /// </summary>
        public static void DrawHorizontalLine(float height = 1f, float margin = 2f)
        {
            var rect = EditorGUILayout.GetControlRect(false, height + margin * 2);
            rect.y += margin;
            rect.height = height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        /// カウントバッジを描画
        /// </summary>
        public static void DrawCountBadge(int count, Color color)
        {
            var content = new GUIContent(count.ToString());
            var size = CountBadgeStyle.CalcSize(content);
            
            var rect = GUILayoutUtility.GetRect(size.x, size.y, CountBadgeStyle);
            
            // 背景
            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.backgroundColor = bgColor;
            
            // テキスト
            GUI.Label(rect, content, CountBadgeStyle);
        }

        /// <summary>
        /// アセットアイコン付きボタンを描画
        /// </summary>
        public static bool DrawAssetButton(Object asset, string label, GUIStyle style = null)
        {
            if (asset == null) return false;
            
            style = style ?? EditorStyles.label;
            
            var icon = AssetUtils.GetAssetIcon(asset);
            var content = new GUIContent(label, icon);
            
            return GUILayout.Button(content, style);
        }

        /// <summary>
        /// 小さいアセットアイコン付きボタンを描画
        /// </summary>
        public static bool DrawSmallAssetButton(Object asset, string label)
        {
            if (asset == null) return false;
            
            var icon = AssetUtils.GetAssetIcon(asset);
            
            // アイコンを小さくリサイズしたGUIContentを作成
            var content = new GUIContent(label);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // アイコンを固定サイズで描画
                if (icon != null)
                {
                    var iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }
                
                // ラベルボタン
                return GUILayout.Button(label, SmallAssetLabelStyle);
            }
        }

        /// <summary>
        /// コンテキストメニューを表示
        /// </summary>
        public static void ShowAssetContextMenu(Object asset, string assetPath)
        {
            if (asset == null) return;
            
            var menu = new GenericMenu();
            
            // 選択
            menu.AddItem(new GUIContent("選択"), false, () => AssetUtils.SelectAndPing(asset));
            
            menu.AddSeparator("");
            
            // パスコピー
            menu.AddItem(new GUIContent("パスをコピー"), false, () => AssetUtils.CopyPathToClipboard(assetPath));
            
            // 名前コピー
            menu.AddItem(new GUIContent("名前をコピー"), false, () => 
            {
                GUIUtility.systemCopyBuffer = asset.name;
                Debug.Log($"Copied to clipboard: {asset.name}");
            });
            
            // GUIDコピー
            menu.AddItem(new GUIContent("GUIDをコピー"), false, () =>
            {
                var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
                GUIUtility.systemCopyBuffer = guid.ToString();
                Debug.Log($"Copied to clipboard: {guid}");
            });
            
            menu.AddSeparator("");
            
            // エクスプローラーで表示
            menu.AddItem(new GUIContent("エクスプローラーで表示"), false, () =>
            {
                EditorUtility.RevealInFinder(assetPath);
            });
            
            menu.ShowAsContext();
        }

        /// <summary>
        /// プログレスバーを表示
        /// </summary>
        public static void DrawProgressBar(string title, string info, float progress)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, progress, $"{title} - {info}");
        }
    }
}