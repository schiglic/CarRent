using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace CarRent
{
    public partial class UserInfo : Window
    {
        private readonly CarRentDbContext _context;
        private readonly int _userId;

        public UserInfo(int userId)
        {
            InitializeComponent();
            _userId = userId;

            // Налаштування підключення до бази даних
            var optionsBuilder = new DbContextOptionsBuilder<CarRentDbContext>();
            optionsBuilder.UseNpgsql("Host=ep-broad-fire-a95akvsv-pooler.gwc.azure.neon.tech;Database=CarRentDB;Username=owner;Password=npg_MsWmoTr40JNf;SSL Mode=Require");
            _context = new CarRentDbContext(optionsBuilder.Options);

            LoadUserInfo();
            LoadBookingHistory();
        }

        private void LoadUserInfo()
        {
            // Завантажуємо інформацію про користувача
            var user = _context.Users.FirstOrDefault(u => u.Id == _userId);
            if (user != null)
            {
                FirstNameTextBlock.Text = user.FirstName;
                LastNameTextBlock.Text = user.LastName;
                LoginTextBlock.Text = user.Login;
                PhoneTextBlock.Text = user.Phone;
            }
            else
            {
                MessageBox.Show("User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadBookingHistory()
        {
            // Завантажуємо історію замовлень користувача разом із пов’язаною інформацією про машину
            var bookings = _context.Bookings
                .Include(b => b.Car) // Завантажуємо пов’язану машину
                .Where(b => b.UserId == _userId)
                .ToList();
            BookingsDataGrid.ItemsSource = bookings;
        }
    }
}