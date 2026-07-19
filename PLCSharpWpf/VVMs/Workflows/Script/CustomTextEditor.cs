
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using OpenCvSharp;
using RoslynPad.Editor;
using RoslynPad.Roslyn;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Document = Microsoft.CodeAnalysis.Document;

namespace PLCSharp.VVMs.Workflows.Script
{
    /// <summary>
    /// CustomTextEditor
    /// </summary>
    public class CustomTextEditor : RoslynCodeEditor
    {
        private static Document _document;

        private RoslynHost _host;
        private string _pendingText;

        /// <summary>
        /// 最小/最大字体大小
        /// </summary>
        const double MinFontSize = 8;
        const double MaxFontSize = 72;
        const double ZoomStep = 2;

        /// <summary>
        /// 光标位置变化事件（用于状态栏更新）
        /// </summary>
        public event EventHandler<(int Line, int Column)> CaretPositionChanged;

        /// <summary>
        /// 字体大小变化事件（用于状态栏更新）
        /// </summary>
        public event EventHandler<double> FontSizeChanged;

        /// <summary>
        /// CustomTextEditor
        /// </summary>
        public CustomTextEditor()
        {
            TextArea.KeyDown += TextBoxEditor_KeyDown;
            TextArea.LostKeyboardFocus += TextArea_LostKeyboardFocus;
            TextArea.GotKeyboardFocus += TextArea_GotKeyboardFocus;
            TextArea.MouseWheel += TextArea_MouseWheel;
            ICSharpCode.AvalonEdit.Search.SearchPanel.Install(this);
            DataContextChanged += OnDataContextChanged;
            TextArea.Caret.PositionChanged += CaretOnPositionChanged;
            Loaded += (s, e) => SetValue(ICSharpCode.AvalonEdit.TextEditor.FontSizeProperty, FontSize);
        }



        private int caretOffset;

        private void CaretOnPositionChanged(object sender, EventArgs e)
        {
            if (coding)
            {
                caretOffset = CaretOffset;
                CaretPositionChanged?.Invoke(this, (TextArea.Caret.Line, TextArea.Caret.Column));
            }
        }

        /// <summary>
        /// 跳转到指定行
        /// </summary>
        /// <param name="line">行号（从1开始）</param>
        public void GoToLine(int line)
        {
            if (line < 1 || line > Document.LineCount) return;
            var textLine = Document.GetLineByNumber(line);
            CaretOffset = textLine.Offset;
            ScrollToLine(line);
            TextArea.Caret.BringCaretToView();
            Focus();
        }
#nullable enable
#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs args)
#pragma warning restore VSTHRD100 // Avoid async void methods
#pragma warning restore IDE0079 // 请删除不必要的忽略
        {
            try
            {

                var workingDirectory = Directory.GetCurrentDirectory();

                _host = new CustomRoslynHost(additionalAssemblies:
                [
                        Assembly.Load("RoslynPad.Roslyn.Windows"),
                    Assembly.Load("RoslynPad.Editor.Windows"),

            ],
                RoslynHostReferences.NamespaceDefault.With(assemblyReferences:
                [
                    typeof(object).Assembly,
                typeof(Mat).Assembly,
                typeof(App).Assembly,
                ]
                ));


                var documentId = await InitializeAsync(_host, new ClassificationHighlightColors(),
                    workingDirectory, string.Empty, SourceCodeKind.Script);

                _document = _host.GetDocument(documentId);

                // 如果有初始化前暂存的文本，现在应用
                if (_pendingText != null && Document != null)
                {
                    Document.Text = _pendingText;
                    Text = _pendingText;
                    _pendingText = null;
                }

            }                // 原有的代码

            catch (Exception)
            {

            }
        }

#nullable disable
        bool coding;
        private void TextArea_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            coding = true;
        }
        private void TextArea_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            coding = false;

        }
        private void TextBoxEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _ = FormatCodeAsync();
                e.Handled = true;
                return;
            }

            // Ctrl+滚轮缩放快捷键
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.OemPlus:
                    case Key.Add:
                        ZoomIn();
                        e.Handled = true;
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        ZoomOut();
                        e.Handled = true;
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        ResetZoom();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void TextArea_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (e.Delta > 0)
                    ZoomIn();
                else
                    ZoomOut();
            }
        }

        /// <summary>
        /// 放大
        /// </summary>
        public void ZoomIn()
        {
            var newSize = Math.Min(FontSize + ZoomStep, MaxFontSize);
            FontSize = newSize;
        }

        /// <summary>
        /// 缩小
        /// </summary>
        public void ZoomOut()
        {
            var newSize = Math.Max(FontSize - ZoomStep, MinFontSize);
            FontSize = newSize;
        }

        /// <summary>
        /// 重置缩放
        /// </summary>
        public void ResetZoom()
        {
            FontSize = 20;
        }

        /// <summary>
        /// FontSize 依赖属性（支持绑定和事件通知）
        /// </summary>
        public new double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public new static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(CustomTextEditor),
                new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFontSizeChanged));

        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editor = (CustomTextEditor)d;
            var newSize = (double)e.NewValue;
            // 设置到 WPF 控件的 FontSize 依赖属性上使渲染生效
            // （这是基类 Control.FontSizeProperty，不是本类的 FontSizeProperty，不会递归）
            editor.SetValue(ICSharpCode.AvalonEdit.TextEditor.FontSizeProperty, newSize);
            editor.FontSizeChanged?.Invoke(editor, newSize);
        }
        /// <summary>
        /// FormatCodeAsync
        /// </summary>
        /// <returns>返回结果</returns>
        public async Task FormatCodeAsync()
        {
            try
            {
                var textDocument = _document.WithText(SourceText.From(Document.Text));

                var formatDoc = await Formatter.FormatAsync(textDocument);

                var text = await formatDoc.GetTextAsync();

                Document.Text = text.ToString();
                Text = Document.Text;
            }
            catch
            {
                // 代码有语法错误时跳过格式化，避免崩溃
            }
        }

        /// <summary>
        /// 在光标位置插入代码（保留撤销历史和光标位置）
        /// </summary>
        /// <param name="str">str</param>
        public void Insert(string str)
        {
            var offset = CaretOffset;
            Document.Insert(offset, str);
            CaretOffset = offset + str.Length;
            Text = Document.Text;
        }

        // <summary>
        /// A bindable Text property
        /// </summary>
        /// <summary>
        /// Text
        /// </summary>
        public new string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// TextProperty
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(CustomTextEditor),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = default(string),
                    BindsTwoWayByDefault = true,
                    PropertyChangedCallback = OnDependencyPropertyChanged
                }
            );

        /// <summary>
        /// 属性改变回调
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="args"></param>
        protected static void OnDependencyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var target = (CustomTextEditor)obj;
            var newValue = (args.NewValue ?? "").ToString();

            if (target.Document != null)
            {
                target.Document.Text = newValue;
            }
            else
            {
                // Document 尚未初始化，暂存值，初始化后应用
                target._pendingText = newValue;
            }
        }
    }

    /// <summary>
    /// CustomRoslynHost
    /// </summary>
    public class CustomRoslynHost : RoslynHost
    {
        private bool _addedAnalyzers;

#nullable enable
        /// <summary>
        /// CustomRoslynHost
        /// </summary>
        public CustomRoslynHost(IEnumerable<Assembly>? additionalAssemblies = null, RoslynHostReferences? references = null, ImmutableHashSet<string>? disabledDiagnostics = null) : base(additionalAssemblies, references, disabledDiagnostics)
        {
        }

#nullable disable

        /// <summary>
        /// 获取SolutionAnalyzerReferences
        /// </summary>
        /// <returns>返回结果</returns>
        protected override IEnumerable<AnalyzerReference> GetSolutionAnalyzerReferences()
        {
            if (!_addedAnalyzers)
            {
                _addedAnalyzers = true;
                return base.GetSolutionAnalyzerReferences();
            }

            return [];
        }
    }
}