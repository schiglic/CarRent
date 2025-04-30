using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CarRent
{
    public partial class MainWindow : Window
    {
        private readonly CarRentDbContext _context;
        private readonly User _currentUser;

        public MainWindow(User currentUser = null)
        {
            InitializeComponent();
            // Налаштування підключення до бази даних
            var optionsBuilder = new DbContextOptionsBuilder<CarRentDbContext>();
            optionsBuilder.UseNpgsql("Host=ep-broad-fire-a95akvsv-pooler.gwc.azure.neon.tech;Database=CarRentDB;Username=owner;Password=npg_MsWmoTr40JNf;SSL Mode=Require");
            _context = new CarRentDbContext(optionsBuilder.Options);

            _currentUser = currentUser;
            DisplayCurrentUser();

            // Завантаження даних при запуску
            LoadCars();
        }

        private void DisplayCurrentUser()
        {
            if (_currentUser != null)
            {
                UserNameTextBlock.Text = $"{_currentUser.FirstName} {_currentUser.LastName}";
                LogoutButton.Visibility = Visibility.Visible; // Показуємо кнопку для залогінених користувачів
                if (!string.IsNullOrEmpty(_currentUser.AvatarPath))
                {
                    // Отримуємо шлях до кореня проєкту (CarRent)
                    string projectRoot = Directory.GetCurrentDirectory();
                    while (System.IO.Path.GetFileName(projectRoot) != "CarRent" && !string.IsNullOrEmpty(projectRoot))
                    {
                        projectRoot = Directory.GetParent(projectRoot)?.FullName;
                    }
                    if (string.IsNullOrEmpty(projectRoot))
                    {
                        MessageBox.Show("Could not find project root directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Формуємо повний шлях до аватарки (CarRent/avatars/username.jpg)
                    string avatarFullPath = System.IO.Path.Combine(projectRoot, _currentUser.AvatarPath);
                    if (File.Exists(avatarFullPath))
                    {
                        try
                        {
                            UserAvatarImage.Source = new BitmapImage(new Uri(avatarFullPath));
                        }
                        catch
                        {
                            MessageBox.Show("Failed to load avatar image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Avatar image not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                UserNameTextBlock.Text = "Guest";
                LogoutButton.Visibility = Visibility.Collapsed; // Приховуємо кнопку для гостей
            }
        }

        private void UserAvatarImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser != null)
            {
                // Відкриваємо вікно UserInfo із передачею UserId
                var userInfoWindow = new UserInfo(_currentUser.Id);
                userInfoWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No user is logged in.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Відкриваємо вікно Register
            var registerWindow = new Register();
            registerWindow.Show();
            // Закриваємо поточне вікно MainWindow
            Close();
        }

        public void LoadCars()
        {
            // Завантажуємо машини із бази даних
            var cars = _context.Cars.ToList();
            // Прив’язуємо дані до ListBox
            CarsListBox.ItemsSource = cars;
        }

        private void CarsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CarsListBox.SelectedItem is Car selectedCar)
            {
                // Передаємо поточного користувача у CarDetailsWindow
                var detailsWindow = new CarDetailsWindow(selectedCar, _currentUser);
                detailsWindow.ShowDialog();
            }
        }
    }

    // Клас контексту бази даних
    public class CarRentDbContext : DbContext
    {
        public CarRentDbContext(DbContextOptions<CarRentDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Налаштування назв таблиць (у нижньому регістрі, як у базі)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Car>().ToTable("cars");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<Payment>().ToTable("payments");

            // Налаштування назв колонок (у нижньому регістрі, як у базі)
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.Login).HasColumnName("login");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.AvatarPath).HasColumnName("avatar_path");
            });

            modelBuilder.Entity<Car>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Brand).HasColumnName("brand");
                entity.Property(e => e.Model).HasColumnName("model");
                entity.Property(e => e.Year).HasColumnName("year");
                entity.Property(e => e.LicensePlate).HasColumnName("license_plate");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.PricePerDay).HasColumnName("price_per_day");
                entity.Property(e => e.ImagePath).HasColumnName("image_path");
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CarId).HasColumnName("car_id");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.TotalPrice).HasColumnName("total_price");
                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.BookingId).HasColumnName("booking_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Amount).HasColumnName("amount");
                entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
                entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            });
        }
    }

    // Моделі для таблиць
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string AvatarPath { get; set; }
    }

    public class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string LicensePlate { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public decimal PricePerDay { get; set; }
        public string ImagePath { get; set; }
    }

    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int CarId { get; set; }
        public Car Car { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }
    }
}