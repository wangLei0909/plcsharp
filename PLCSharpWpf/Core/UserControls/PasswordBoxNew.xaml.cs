using System.Windows;
using System.Windows.Controls;

namespace PLCSharp.Core.UserControls
{
    /// <summary>
    /// PasswordBoxNew.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordBoxNew : UserControl
    {
        /// <summary>
        /// 密码Box新建
        /// </summary>
        public PasswordBoxNew()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Text = ((PasswordBox)sender).Password;
        }



        /// <summary>
        /// Text
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        /// <summary>
        /// TextProperty
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(PasswordBoxNew), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, TextSourceChangedCallback));

        private static void TextSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = ((PasswordBoxNew)d);

            if (obj.Text == "")
                obj.passwordBox.Password = obj.Text;

        }
    }
}
