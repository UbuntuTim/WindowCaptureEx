using System.Configuration;
using System.Data;
using System.Windows;

namespace WindowCaptureExample
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var main = new MainWindow();
            var second = new SecondWindow();
            var third = new ThirdWindow();

            main.Show();
            second.Show();
            third.Show();
        }
    }

}
