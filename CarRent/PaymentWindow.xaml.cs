using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace CarRent
{
    public partial class PaymentWindow : Window
    {
        private readonly Booking _booking;
        private readonly int _carId;
        private Car _car;
        private readonly CarRentDbContext _context;

        public PaymentWindow(Booking booking, int carId)
        {
            InitializeComponent();
            _booking = booking;
            _carId = carId;

            // Налаштування підключення до бази даних
            var optionsBuilder = new DbContextOptionsBuilder<CarRentDbContext>();
            optionsBuilder.UseNpgsql("Host=ep-broad-fire-a95akvsv-pooler.gwc.azure.neon.tech;Database=CarRentDB;Username=owner;Password=npg_MsWmoTr40JNf;SSL Mode=Require");
            _context = new CarRentDbContext(optionsBuilder.Options);

            // Завантажуємо Car із бази даних
            _car = _context.Cars.FirstOrDefault(c => c.Id == _carId);
            if (_car == null)
            {
                MessageBox.Show("Error: Car not found in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            // Відображення сьогоднішньої дати
            TodayDateTextBlock.Text = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            // Встановлюємо початкові значення для дат і обмеження
            StartDatePicker.DisplayDateStart = DateTime.UtcNow.Date;
            EndDatePicker.DisplayDateStart = DateTime.UtcNow.Date;
            StartDatePicker.SelectedDate = DateTime.UtcNow.Date;
            EndDatePicker.SelectedDate = DateTime.UtcNow.Date.AddDays(1);

            // Оновлюємо TotalPrice на основі початкових дат
            UpdateTotalPrice();

            DataContext = this;

            // Встановлюємо початкове значення для способу оплати
            PaymentMethodComboBox.SelectedIndex = 0;

            // Додаємо обробники подій для оновлення TotalPrice при зміні дат
            StartDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            EndDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
        }

        public Booking Booking => _booking;
        public Car Car => _car;

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
            {
                var startDate = StartDatePicker.SelectedDate.Value;
                var endDate = EndDatePicker.SelectedDate.Value;

                // Перевіряємо, чи кінцева дата не раніше початкової
                if (endDate < startDate)
                {
                    MessageBox.Show("End date cannot be earlier than start date.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    EndDatePicker.SelectedDate = startDate.AddDays(1);
                    return;
                }

                // Розраховуємо кількість днів оренди
                var rentalDays = (endDate - startDate).Days + 1; // Включаємо перший день
                _booking.TotalPrice = _car.PricePerDay * rentalDays;

                // Оновлюємо відображення
                DataContext = null;
                DataContext = this;
            }
        }

        private void ConfirmPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            string paymentMethod = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(paymentMethod))
            {
                MessageBox.Show("Please select a payment method.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select start and end dates.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Використовуємо транзакцію для атомарності
                using var transaction = _context.Database.BeginTransaction();

                // Оновлюємо дати в Booking, конвертуючи в UTC
                _booking.StartDate = StartDatePicker.SelectedDate.Value.ToUniversalTime();
                _booking.EndDate = EndDatePicker.SelectedDate.Value.ToUniversalTime();

                // Додаємо бронювання
                _context.Bookings.Add(_booking);
                _context.SaveChanges();

                // Додаємо платіж
                var payment = new Payment
                {
                    BookingId = _booking.Id,
                    UserId = _booking.UserId,
                    Amount = _booking.TotalPrice,
                    PaymentMethod = paymentMethod,
                    Status = "Completed",
                    PaymentDate = DateTime.UtcNow, // Уже в UTC
                    TransactionId = Guid.NewGuid().ToString()
                };
                _context.Payments.Add(payment);

                // Оновлюємо статус бронювання
                _booking.Status = "Confirmed";

                // Оновлюємо статус машини
                _car.Status = "Booked";
                _context.Cars.Update(_car);

                // Зберігаємо всі зміни
                _context.SaveChanges();

                // Підтверджуємо транзакцію
                transaction.Commit();

                // Показуємо повідомлення про успішну оплату
                PaymentStatusTextBlock.Text = "Payment successful!";
                PaymentStatusTextBlock.Visibility = Visibility.Visible;
                ConfirmPaymentButton.IsEnabled = false;

                // Оновлюємо список машин у MainWindow
                if (Owner is MainWindow mainWindow)
                {
                    mainWindow.LoadCars();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing payment: {ex.Message}\nInner Exception: {ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}