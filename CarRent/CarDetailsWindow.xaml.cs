using System.Windows;

namespace CarRent
{
    public partial class CarDetailsWindow : Window
    {
        public CarDetailsWindow(Car car)
        {
            InitializeComponent();
            DataContext = car;
        }
    }
}