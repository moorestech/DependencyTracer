using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace DependencyTracer
{
    /// <summary>
    /// DependencyTracerをDLLとしてエクスポートするツール
    /// </summary>
    public class DependencyTracerExporter : EditorWindow
    {
        private const string PackageFolderName = "DependencyTracerPackages";
        private const string AssemblyName = "DependencyTracer";
        private string _outputPath = "";
        private bool _includeXmlDoc = true;
        private bool _includePdb = true;
        
        [MenuItem("Tools/Dependency Tracer/Export as DLL")]
        public static void ShowWindow()
        {
            var window = GetWindow<DependencyTracerExporter>("DependencyTracer Exporter");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }
        
        private void OnEnable()
        {
            // デフォルトの出力パスを設定
            _outputPath = Path.Combine(Application.dataPath, PackageFolderName);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("DependencyTracer DLL エクスポーター", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 出力パス設定
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("出力フォルダ:", GUILayout.Width(80));
            _outputPath = EditorGUILayout.TextField(_outputPath);
            if (GUILayout.Button("参照", GUILayout.Width(50)))
            {
                var selectedPath = EditorUtility.OpenFolderPanel("出力フォルダを選択", _outputPath, "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _outputPath = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // オプション
            _includeXmlDoc = EditorGUILayout.Toggle("XMLドキュメントを含める", _includeXmlDoc);
            _includePdb = EditorGUILayout.Toggle("デバッグシンボル(PDB)を含める", _includePdb);
            
            EditorGUILayout.Space();
            
            // 情報表示
            EditorGUILayout.HelpBox(
                "このツールは以下のファイルをエクスポートします:\n" +
                "• DependencyTracer.dll - メインアセンブリ\n" +
                (_includeXmlDoc ? "• DependencyTracer.xml - XMLドキュメント\n" : "") +
                (_includePdb ? "• DependencyTracer.pdb - デバッグシンボル\n" : "") +
                "• README.txt - 使用方法",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // エクスポートボタン
            GUI.enabled = !string.IsNullOrEmpty(_outputPath);
            if (GUILayout.Button("DLLをエクスポート", GUILayout.Height(30)))
            {
                ExportDLL();
            }
            GUI.enabled = true;
        }
        
        private void ExportDLL()
        {
            try
            {
                // 出力ディレクトリを作成
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }
                
                // コンパイル済みアセンブリを検索
                var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
                var targetAssembly = assemblies.FirstOrDefault(a => a.name == AssemblyName);
                
                if (targetAssembly == null)
                {
                    EditorUtility.DisplayDialog("エラー", 
                        $"{AssemblyName}アセンブリが見つかりません。\n" +
                        "アセンブリ定義ファイル(.asmdef)が正しく設定されているか確認してください。", 
                        "OK");
                    return;
                }
                
                // DLLのパス
                var dllPath = targetAssembly.outputPath;
                if (!File.Exists(dllPath))
                {
                    EditorUtility.DisplayDialog("エラー", 
                        $"コンパイル済みDLLが見つかりません:\n{dllPath}", 
                        "OK");
                    return;
                }
                
                var copiedFiles = new List<string>();
                
                // DLLをコピー
                var outputDllPath = Path.Combine(_outputPath, Path.GetFileName(dllPath));
                File.Copy(dllPath, outputDllPath, true);
                copiedFiles.Add(Path.GetFileName(dllPath));
                
                // XMLドキュメントをコピー（存在する場合）
                if (_includeXmlDoc)
                {
                    var xmlPath = Path.ChangeExtension(dllPath, ".xml");
                    if (File.Exists(xmlPath))
                    {
                        var outputXmlPath = Path.Combine(_outputPath, Path.GetFileName(xmlPath));
                        File.Copy(xmlPath, outputXmlPath, true);
                        copiedFiles.Add(Path.GetFileName(xmlPath));
                    }
                }
                
                // PDBをコピー（存在する場合）
                if (_includePdb)
                {
                    var pdbPath = Path.ChangeExtension(dllPath, ".pdb");
                    if (File.Exists(pdbPath))
                    {
                        var outputPdbPath = Path.Combine(_outputPath, Path.GetFileName(pdbPath));
                        File.Copy(pdbPath, outputPdbPath, true);
                        copiedFiles.Add(Path.GetFileName(pdbPath));
                    }
                }
                
                // READMEファイルを作成
                CreateReadmeFile(_outputPath);
                copiedFiles.Add("README.txt");
                
                // 成功メッセージ
                var message = $"エクスポート完了!\n\n" +
                             $"出力先: {_outputPath}\n\n" +
                             $"エクスポートされたファイル:\n" +
                             string.Join("\n", copiedFiles.Select(f => $"• {f}"));
                
                EditorUtility.DisplayDialog("成功", message, "OK");
                
                // エクスプローラーで開く
                EditorUtility.RevealInFinder(_outputPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("エラー", 
                    $"エクスポート中にエラーが発生しました:\n{ex.Message}", 
                    "OK");
                Debug.LogException(ex);
            }
        }
        
        private void CreateReadmeFile(string outputPath)
        {
            var readmePath = Path.Combine(outputPath, "README.txt");
            var content = @"DependencyTracer - Unity依存関係追跡ツール
==========================================

■ 概要
DependencyTracerは、Unityプロジェクト内のアセット間の依存関係を
視覚的に表示・追跡するためのエディタ拡張ツールです。

■ インストール方法

1. DLLを使用する場合:
   - DependencyTracer.dll を Assets/Editor フォルダ内の任意の場所にコピー
   - 必要に応じて .xml (ドキュメント) と .pdb (デバッグシンボル) もコピー

2. ソースコードから使用する場合:
   - ソースコードを Assets/Editor フォルダ内にコピー
   - DependencyTracer.asmdef をソースコードと同じフォルダに配置

■ 使用方法

1. メニューから開く:
   Window → Dependency Tracer

2. アセットの依存関係を表示:
   プロジェクトビューでアセットを右クリック → Show Dependencies

3. 機能:
   - 依存先: 選択したアセットが依存している他のアセット
   - 参照元: 選択したアセットを参照している他のアセット
   - 全体解析: プロジェクト全体の依存関係を解析してデータベースを構築

■ 注意事項
- 初回使用時は「全体解析」を実行してデータベースを構築してください
- 大規模プロジェクトでは解析に時間がかかる場合があります
- データベースは Library/DependencyTracerDB.dat に保存されます

■ バージョン情報
エクスポート日: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + @"

■ ライセンス
このツールはMITライセンスで提供されています。
";
            File.WriteAllText(readmePath, content, System.Text.Encoding.UTF8);
        }

    }
}