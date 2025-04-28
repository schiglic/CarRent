using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace CarRent
{
    public partial class Register : Window
    {
        private readonly CarRentDbContext _context;

        public Register()
        {
            InitializeComponent();
            // Налаштування підключення до бази даних
            var optionsBuilder = new DbContextOptionsBuilder<CarRentDbContext>();
            optionsBuilder.UseNpgsql("Host=ep-broad-fire-a95akvsv-pooler.gwc.azure.neon.tech;Database=CarRentDB;Username=owner;Password=npg_MsWmoTr40JNf;SSL Mode=Require");
            _context = new CarRentDbContext(optionsBuilder.Options);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginUsernameTextBox.Text;
            string password = LoginPasswordBox.Password;

            // Перевірка, чи користувач існує
            var user = _context.Users.FirstOrDefault(u => u.Login == username && u.Password == password);

            if (user != null)
            {
                // Успішний вхід, відкриваємо MainWindow
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Close(); // Закриваємо вікно входу/реєстрації
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string firstName = RegisterFirstNameTextBox.Text;
            string lastName = RegisterLastNameTextBox.Text;
            string username = RegisterUsernameTextBox.Text;
            string password = RegisterPasswordBox.Password;
            string phone = RegisterPhoneTextBox.Text;

            // Перевірка, чи всі поля заповнені
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Please fill in all fields.", "Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Перевірка, чи логін уже існує
            if (_context.Users.Any(u => u.Login == username))
            {
                MessageBox.Show("Username already exists.", "Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Створення нового користувача
            var newUser = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Login = username,
                Password = password, // У реальному додатку пароль потрібно хешувати!
                Phone = phone
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            MessageBox.Show("Registration successful! You can now log in.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Очищення полів після реєстрації
            RegisterFirstNameTextBox.Text = "";
            RegisterLastNameTextBox.Text = "";
            RegisterUsernameTextBox.Text = "";
            RegisterPasswordBox.Password = "";
            RegisterPhoneTextBox.Text = "";
        }
    }
}