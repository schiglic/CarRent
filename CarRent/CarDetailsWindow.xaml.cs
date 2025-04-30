using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace CarRent
{
    public partial class CarDetailsWindow : Window
    {
        private readonly Car _selectedCar;
        private readonly User _currentUser;
        private readonly CarRentDbContext _context;

        public CarDetailsWindow(Car selectedCar, User currentUser)
        {
            InitializeComponent();
            _selectedCar = selectedCar;
            _currentUser = currentUser;
            DataContext = _selectedCar;

            // Налаштування підключення до бази даних
            var optionsBuilder = new DbContextOptionsBuilder<CarRentDbContext>();
            optionsBuilder.UseNpgsql("Host=ep-broad-fire-a95akvsv-pooler.gwc.azure.neon.tech;Database=CarRentDB;Username=owner;Password=npg_MsWmoTr40JNf;SSL Mode=Require");
            _context = new CarRentDbContext(optionsBuilder.Options);
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            // Перевіряємо, чи є авторизований користувач
            if (_currentUser == null)
            {
                MessageBox.Show("Please log in to make a booking.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Завантажуємо пов’язані об’єкти
                var car = _context.Cars.FirstOrDefault(c => c.Id == _selectedCar.Id);
                var user = _context.Users.FirstOrDefault(u => u.Id == _currentUser.Id);

                if (car == null || user == null)
                {
                    MessageBox.Show("Error: Car or user not found in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Перевіряємо, чи машина доступна
                if (car.Status != "Available")
                {
                    MessageBox.Show("This car is not available for booking.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Створюємо бронювання, але НЕ встановлюємо навігаційні властивості
                var booking = new Booking
                {
                    UserId = user.Id,
                    CarId = car.Id,
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddDays(1),
                    TotalPrice = car.PricePerDay,
                    Status = "Pending"
                };

                // Передаємо CarId
                var paymentWindow = new PaymentWindow(booking, car.Id);
                paymentWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing booking: {ex.Message}\nInner Exception: {ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}