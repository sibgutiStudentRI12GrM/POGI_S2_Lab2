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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace lab2 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        
        public MainWindow() {
            InitializeComponent();
        }

        private void Btn_OpenServerWindow_Click(object sender, RoutedEventArgs e) {
            ServerWindow window = new ServerWindow();
            window.Show();
        }

        private void Btn_OpenClientWindow_Click(object sender, RoutedEventArgs e) {
            ClientWindow window = new ClientWindow();
            window.Show();
        }
    }
}
