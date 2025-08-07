using Cobrinhada;
using System.Windows;

namespace C0BR1NHADA
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Ativa modo tela cheia ao abrir o menu principal
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var gameWindow = new GameWindow();
            gameWindow.Show();
            this.Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
