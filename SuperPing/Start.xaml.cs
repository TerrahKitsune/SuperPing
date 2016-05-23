using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuperPing
{
    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : Window
    {
        public Start()
        {
            InitializeComponent();
            this.Loaded += Start_Loaded;
        }

        void Start_Loaded(object sender, RoutedEventArgs e)
        {
            this.KeyUp += TextBox_KeyUp;
        }

        private void Ok_click(object sender, RoutedEventArgs e)
        {
            MainWindow main = this.Tag as MainWindow;
            main.addr = AddressField.Text;
            this.Close();
        }
        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            Ok_click(null,null);
        }
    }
}
