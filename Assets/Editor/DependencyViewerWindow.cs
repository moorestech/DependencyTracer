using System.Collections.Generic;
using System.Linq;
using DependencyTracer.Core;
using DependencyTracer.UI;
using UnityEditor;
using UnityEngine;

namespace DependencyTracer
{
    /// <summary>
    /// 依存関係を表示するメインウィンドウ
    /// </summary>
    public class DependencyViewerWindow : EditorWindow
    {
        private static DependencyViewerWindow _instance;
        private static bool _refreshRequested;

        [SerializeField]
        private List<AssetItem> _assetItems = new List<AssetItem>();
        
        [SerializeField]
        private Vector2 _scrollPosition;
        
        [SerializeField]
        private bool _showDependencies = true;
        
        [SerializeField]
        private bool _showReferences = true;
        
        [SerializeField]
        private bool _showIndirectDependencies = false;
        
        [SerializeField]
        private bool _showHierarchicalView = true;

        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;

        /// <summary>
        /// ウィンドウを開く
        /// </summary>
        [MenuItem("Window/Dependency Tracer")]
        public static DependencyViewerWindow ShowWindow()
        {
            _instance = GetWindow<DependencyViewerWindow>("Dependency Tracer");
            return _instance;
        }

        /// <summary>
        /// アセット右クリックメニュー - 依存関係を表示
        /// </summary>
        [MenuItem("Assets/Show Dependencies", true)]
        private static bool ValidateShowDependencies()
        {
            return Selection.objects.Length > 0;
        }

        [MenuItem("Assets/Show Dependencies")]
        private static void ShowDependencies()
        {
            ShowWindow();
            _instance.AddAssets(Selection.objects);
        }

        /// <summary>
        /// 更新をリクエスト
        /// </summary>
        public static void RequestRefresh()
        {
            _refreshRequested = true;
            if (_instance != null)
            {
                _instance.Repaint();
            }
        }

        private void OnEnable()
        {
            _instance = this;
            titleContent = new GUIContent("Dependency Tracer", EditorGUIUtility.IconContent("d_SceneViewTools").image);
            minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            InitializeStyles();
            DrawToolbar();
            DrawMainContent();

            // 更新リクエストの処理
            if (_refreshRequested)
            {
                _refreshRequested = false;
                RefreshAllItems();
            }
        }

        private void InitializeStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    margin = new RectOffset(10, 5, 3, 3)
                };
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 解析ボタン
                if (GUILayout.Button(new GUIContent("全体解析", "すべてのアセットの依存関係を解析します"), EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog(
                        "依存関係の全体解析",
                        "プロジェクト内のすべてのアセットの依存関係を解析します。\n" +
                        "この処理には時間がかかる場合があります。\n\n" +
                        "続行しますか？",
                        "実行", "キャンセル"))
                    {
                        DependencyDatabase.AnalyzeAllAssets();
                        RefreshAllItems();
                    }
                }

                // データベース情報ボタン
                if (GUILayout.Button(new GUIContent("DB情報", "データベースの状態を表示"), EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    DependencyDatabase.ShowDatabaseInfo();
                }

                GUILayout.Space(20);

                // 表示オプション
                _showDependencies = GUILayout.Toggle(_showDependencies, new GUIContent("依存先", "このアセットが依存しているアセット"), EditorStyles.toolbarButton, GUILayout.Width(60));
                _showReferences = GUILayout.Toggle(_showReferences, new GUIContent("参照元", "このアセットを参照しているアセット"), EditorStyles.toolbarButton, GUILayout.Width(60));

                GUILayout.Space(10);

                // 新しいオプション
                _showIndirectDependencies = GUILayout.Toggle(_showIndirectDependencies, new GUIContent("間接依存", "間接的な依存関係も表示"), EditorStyles.toolbarButton, GUILayout.Width(60));
                
                GUILayout.Space(10);
                
                _showHierarchicalView = GUILayout.Toggle(_showHierarchicalView, new GUIContent("階層表示", "ファイル階層で表示"), EditorStyles.toolbarButton, GUILayout.Width(60));
                var listViewContent = new GUIContent("リスト表示", "名前順でリスト表示");
                if (GUILayout.Toggle(!_showHierarchicalView, listViewContent, EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    _showHierarchicalView = false;
                }

                GUILayout.FlexibleSpace();

                // クリアボタン
                if (GUILayout.Button(new GUIContent("クリア", "リストをクリア"), EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    _assetItems.Clear();
                    Repaint();
                }

                // データベース状態の表示
                if (DependencyDatabase.IsInitialized)
                {
                    var label = $"更新: {DependencyDatabase.LastFullAnalysisTime:MM/dd HH:mm}";
                    GUILayout.Label(label, EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label("未初期化", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawMainContent()
        {
            if (!DependencyDatabase.IsInitialized)
            {
                DrawNotInitializedMessage();
                return;
            }

            if (_assetItems.Count == 0)
            {
                DrawEmptyMessage();
                return;
            }

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                for (var i = _assetItems.Count - 1; i >= 0; i--)
                {
                    var item = _assetItems[i];
                    
                    EditorGUILayout.Space(5);
                    
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var shouldRemove = item.DrawGUI(_showDependencies, _showReferences, _showIndirectDependencies, _showHierarchicalView);
                        if (shouldRemove)
                        {
                            _assetItems.RemoveAt(i);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        }

        private void DrawNotInitializedMessage()
        {
            GUILayout.FlexibleSpace();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("データベースが初期化されていません", _headerStyle);
                    GUILayout.Space(10);
                    
                    if (GUILayout.Button("今すぐ解析", GUILayout.Width(100)))
                    {
                        DependencyDatabase.AnalyzeAllAssets();
                        RefreshAllItems();
                    }
                }
                
                GUILayout.FlexibleSpace();
            }
            
            GUILayout.FlexibleSpace();
        }

        private void DrawEmptyMessage()
        {
            GUILayout.FlexibleSpace();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("アセットを選択してください", _headerStyle);
                    GUILayout.Space(5);
                    GUILayout.Label("プロジェクトビューでアセットを右クリック", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.Label("→ 「Show Dependencies」を選択", EditorStyles.centeredGreyMiniLabel);
                }
                
                GUILayout.FlexibleSpace();
            }
            
            GUILayout.FlexibleSpace();
        }

        public void AddAssets(Object[] assets)
        {
            if (!DependencyDatabase.IsInitialized)
            {
                if (EditorUtility.DisplayDialog(
                    "データベース未初期化",
                    "依存関係データベースが初期化されていません。\n今すぐ解析を実行しますか？",
                    "実行", "キャンセル"))
                {
                    DependencyDatabase.AnalyzeAllAssets();
                }
                else
                {
                    return;
                }
            }

            foreach (var asset in assets)
            {
                if (asset == null) continue;

                // 既に追加されているかチェック
                if (_assetItems.Any(item => item.Asset == asset))
                    continue;

                var newItem = new AssetItem(asset);
                _assetItems.Add(newItem);
            }

            Repaint();
        }

        private void RefreshAllItems()
        {
            foreach (var item in _assetItems)
            {
                item.Refresh();
            }
            Repaint();
        }
    }
}