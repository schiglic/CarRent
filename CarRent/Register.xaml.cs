using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;

namespace CarRent
{
    public partial class Register : Window
    {
        private readonly CarRentDbContext _context;
        private string _selectedAvatarPath; // Для зберігання шляху до вибраного файлу аватарки

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
                var mainWindow = new MainWindow(user);
                mainWindow.Show();
                Close(); // Закриваємо вікно входу/реєстрації
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.png)|*.jpg;*.png|All files (*.*)|*.*",
                Title = "Select an avatar image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAvatarPath = openFileDialog.FileName;
                AvatarPathTextBox.Text = System.IO.Path.GetFileName(_selectedAvatarPath);
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

            // Обробка аватарки
            string avatarPath = null;
            if (!string.IsNullOrEmpty(_selectedAvatarPath))
            {
                string avatarsDir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "avatars");
                if (!Directory.Exists(avatarsDir))
                {
                    Directory.CreateDirectory(avatarsDir);
                }

                string extension = System.IO.Path.GetExtension(_selectedAvatarPath);
                string newAvatarFileName = $"{username}{extension}";
                avatarPath = System.IO.Path.Combine("avatars", newAvatarFileName);
                string destinationPath = System.IO.Path.Combine(avatarsDir, newAvatarFileName);

                File.Copy(_selectedAvatarPath, destinationPath, true);
            }

            // Створення нового користувача
            var newUser = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Login = username,
                Password = password, // У реальному додатку пароль потрібно хешувати!
                Phone = phone,
                AvatarPath = avatarPath // Зберігаємо шлях до аватарки
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
            AvatarPathTextBox.Text = "";
            _selectedAvatarPath = null;
        }
    }
}