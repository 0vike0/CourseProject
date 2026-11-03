using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class LoginWindow : Window
    {
        private int _failedAttempts = 0;
        private string _currentCaptcha = "";
        private bool _isReauth = false;

        // 1. Конструктор без параметров (нужен для запуска приложения из App.xaml)
        public LoginWindow()
        {
            InitializeComponent();
            _isReauth = false;
        }

        // 2. Конструктор с параметром (нужен для SessionManager при блокировке)
        public LoginWindow(bool isReauth)
        {
            InitializeComponent();
            _isReauth = isReauth;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtLogin.Text.Trim();
            string password = TxtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                TxtError.Text = "Введите логин и пароль!";
                return;
            }

            // ПРОВЕРКА КАПЧИ (если она видима)
            if (CaptchaPanel.Visibility == Visibility.Visible)
            {
                if (TxtCaptcha.Text.Trim().ToUpper() != _currentCaptcha)
                {
                    TxtError.Text = "Неверный код с картинки!";
                    GenerateCaptcha();
                    TxtCaptcha.Clear();
                    return;
                }
            }

            try
            {
                // 1. Проверяем, существует ли пользователь и не заблокирован ли он
                string checkQuery = "SELECT id, IsBlocked FROM [05.02_User] WHERE Login = @Login";
                DataTable checkResult = DatabaseHelper.ExecuteQuery(checkQuery, new SqlParameter[] { new SqlParameter("@Login", login) });

                if (checkResult.Rows.Count > 0)
                {
                    bool isBlocked = checkResult.Rows[0]["IsBlocked"] != DBNull.Value && Convert.ToBoolean(checkResult.Rows[0]["IsBlocked"]);

                    if (isBlocked)
                    {
                        TxtError.Text = "Ваша учетная запись заблокирована. Обратитесь к администратору.";
                        return;
                    }
                }

                // 2. Проверяем пароль
                string query = "SELECT id, Role, OwnerId FROM [05.02_User] WHERE Login = @Login AND Password = @Password";
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Login", login),
                    new SqlParameter("@Password", password)
                };

                DataTable result = DatabaseHelper.ExecuteQuery(query, parameters);

                if (result.Rows.Count > 0)
                {
                    // УСПЕШНЫЙ ВХОД - сбрасываем счетчик ошибок
                    string resetQuery = "UPDATE [05.02_User] SET FailedAttempts = 0 WHERE Login = @Login";
                    DatabaseHelper.ExecuteNonQuery(resetQuery, new SqlParameter[] { new SqlParameter("@Login", login) });

                    DataRow row = result.Rows[0];
                    int userId = Convert.ToInt32(row["id"]);
                    string role = row["Role"].ToString();
                    int? ownerId = row["OwnerId"] != DBNull.Value ? (int?)Convert.ToInt32(row["OwnerId"]) : null;

                    // ЗАПУСКАЕМ ГЛОБАЛЬНЫЙ ТАЙМЕР БЕЗДЕЙСТВИЯ
                    SessionManager.StartMonitoring();

                    // --- ЛОГИКА ПЕРЕАВТОРИЗАЦИИ ---
                    if (_isReauth)
                    {
                        // Ищем скрытое главное окно и показываем его
                        var existingMain = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                        if (existingMain != null)
                        {
                            existingMain.Show();
                        }
                        else
                        {
                            new MainWindow(userId, role, ownerId).Show();
                        }
                    }
                    else
                    {
                        // Обычный первый запуск
                        MainWindow mainWindow = new MainWindow(userId, role, ownerId);
                        mainWindow.Show();
                    }

                    this.Close();
                }
                else
                {
                    // НЕУДАЧНЫЙ ВХОД
                    _failedAttempts++;

                    string updateQuery = "UPDATE [05.02_User] SET FailedAttempts = ISNULL(FailedAttempts, 0) + 1 WHERE Login = @Login";
                    DatabaseHelper.ExecuteNonQuery(updateQuery, new SqlParameter[] { new SqlParameter("@Login", login) });

                    string countQuery = "SELECT FailedAttempts FROM [05.02_User] WHERE Login = @Login";
                    DataTable countResult = DatabaseHelper.ExecuteQuery(countQuery, new SqlParameter[] { new SqlParameter("@Login", login) });

                    if (countResult.Rows.Count > 0)
                    {
                        int attempts = countResult.Rows[0]["FailedAttempts"] != DBNull.Value ? Convert.ToInt32(countResult.Rows[0]["FailedAttempts"]) : 0;

                        if (attempts >= 3)
                        {
                            string blockQuery = "UPDATE [05.02_User] SET IsBlocked = 1 WHERE Login = @Login";
                            DatabaseHelper.ExecuteNonQuery(blockQuery, new SqlParameter[] { new SqlParameter("@Login", login) });
                            TxtError.Text = "Вы превысили количество попыток. Учетная запись заблокирована!";
                            return;
                        }
                        else
                        {
                            TxtError.Text = $"Неверный пароль. Осталось попыток: {3 - attempts}";
                        }
                    }

                    CaptchaPanel.Visibility = Visibility.Visible;
                    GenerateCaptcha();
                }
            }
            catch (Exception ex)
            {
                TxtError.Text = "Ошибка подключения к серверу: " + ex.Message;
            }
        }

        // МЕТОД ГЕНЕРАЦИИ КАРТИНКИ С КОДОМ
        private void GenerateCaptcha()
        {
            CaptchaCanvas.Children.Clear();
            Random rnd = new Random();
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            _currentCaptcha = "";

            for (int i = 0; i < 4; i++)
            {
                _currentCaptcha += chars[rnd.Next(chars.Length)];
            }

            for (int i = 0; i < _currentCaptcha.Length; i++)
            {
                TextBlock tb = new TextBlock();
                tb.Text = _currentCaptcha[i].ToString();
                tb.FontSize = 20;
                tb.FontWeight = FontWeights.Bold;
                tb.Foreground = new SolidColorBrush(Color.FromRgb(
                    (byte)rnd.Next(50, 150),
                    (byte)rnd.Next(0, 100),
                    (byte)rnd.Next(50, 150)));

                Canvas.SetLeft(tb, 15 + i * 20);
                Canvas.SetTop(tb, rnd.Next(5, 15));
                tb.RenderTransform = new RotateTransform(rnd.Next(-20, 20));

                CaptchaCanvas.Children.Add(tb);
            }

            for (int i = 0; i < 3; i++)
            {
                Line line = new Line();
                line.X1 = rnd.Next(0, 100); line.Y1 = rnd.Next(0, 40);
                line.X2 = rnd.Next(0, 100); line.Y2 = rnd.Next(0, 40);
                line.Stroke = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                line.StrokeThickness = 1;
                CaptchaCanvas.Children.Add(line);
            }

            for (int i = 0; i < 15; i++)
            {
                Ellipse dot = new Ellipse();
                dot.Width = 2; dot.Height = 2;
                dot.Fill = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                Canvas.SetLeft(dot, rnd.Next(0, 100));
                Canvas.SetTop(dot, rnd.Next(0, 40));
                CaptchaCanvas.Children.Add(dot);
            }
        }
    }
}