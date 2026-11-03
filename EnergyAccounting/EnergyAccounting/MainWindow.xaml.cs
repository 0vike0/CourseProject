using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class MainWindow : Window
    {
        public int CurrentUserId { get; set; }
        public string CurrentUserRole { get; set; }
        public int? CurrentOwnerId { get; set; }

        public MainWindow(int userId, string role, int? ownerId)
        {
            InitializeComponent();
            CurrentUserId = userId;
            CurrentUserRole = role;
            CurrentOwnerId = ownerId;

            TxtUserInfo.Text = $"Пользователь: {role}";

            // Настройка видимости кнопок и загрузка дашборда
            if (role == "Operator")
            {
                BtnReports.Visibility = Visibility.Visible;
                BtnTariffs.Visibility = Visibility.Visible;
                LoadAdminDashboard();
            }

            else if (role == "Admin")
            {
                BtnReports.Visibility = Visibility.Visible;
                BtnTariffs.Visibility = Visibility.Visible;
                BtnUsers.Visibility = Visibility.Visible;
                LoadAdminDashboard();
            }
            else if (role == "Owner")
            {
                BtnReadings.Content = "Мои показания";
                LoadOwnerDashboard();
            }
        }

        private void LoadAdminDashboard()
        {
            TxtDashboardTitle.Text = "Общая статистика";
            try
            {
                // Количество квартир
                DataTable dt1 = DatabaseHelper.ExecuteQuery("SELECT COUNT(*) AS Cnt FROM [05.02_Apartment]");
                TxtCard1Title.Text = "Всего квартир";
                TxtCard1Value.Text = dt1.Rows[0]["Cnt"].ToString();

                // Количество собственников
                DataTable dt2 = DatabaseHelper.ExecuteQuery("SELECT COUNT(*) AS Cnt FROM [05.02_Owner]");
                TxtCard2Title.Text = "Собственников";
                TxtCard2Value.Text = dt2.Rows[0]["Cnt"].ToString();

                // Количество показаний за последний месяц
                DataTable dt3 = DatabaseHelper.ExecuteQuery("SELECT COUNT(*) AS Cnt FROM [05.02_Reading] WHERE ReadingDate >= DATEADD(month, -1, GETDATE())");
                TxtCard3Title.Text = "Показаний за месяц";
                TxtCard3Value.Text = dt3.Rows[0]["Cnt"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки статистики: " + ex.Message);
            }
        }

        private void LoadOwnerDashboard()
        {
            TxtDashboardTitle.Text = "Мои данные";
            try
            {
                // Получаем количество квартир у собственника
                SqlParameter[] p = { new SqlParameter("@OwnerId", CurrentOwnerId) };
                DataTable dt1 = DatabaseHelper.ExecuteQuery("SELECT COUNT(*) AS Cnt FROM [05.02_Apartment] WHERE OwnerId = @OwnerId", p);
                TxtCard1Title.Text = "Моих квартир";
                TxtCard1Value.Text = dt1.Rows[0]["Cnt"].ToString();

                // Получаем количество счетчиков в его квартирах
                string queryMeters = @"
                    SELECT COUNT(*) AS Cnt FROM [05.02_Meter] m
                    JOIN [05.02_Apartment] a ON m.ApartmentId = a.id
                    WHERE a.OwnerId = @OwnerId";
                DataTable dt2 = DatabaseHelper.ExecuteQuery(queryMeters, p);
                TxtCard2Title.Text = "Моих счетчиков";
                TxtCard2Value.Text = dt2.Rows[0]["Cnt"].ToString();

                // Скрываем 3-ю карточку, так как она не особо нужна собственнику
                TxtCard3Title.Text = "Дата последнего визита";
                TxtCard3Value.Text = DateTime.Now.ToString("dd.MM.yyyy");
                TxtCard3Value.FontSize = 20; // Уменьшили шрифт для даты
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.StopMonitoring();
            new LoginWindow(false).Show(); // Передаем false
            this.Close(); // При выходе кнопкой - закрываем окно
        }

        private void BtnReadings_Click(object sender, RoutedEventArgs e)
        {
            new ReadingsWindow(CurrentUserRole, CurrentOwnerId).ShowDialog();
            // После закрытия окна ввода можем обновить дашборд
            if (CurrentUserRole == "Owner") LoadOwnerDashboard();
            else LoadAdminDashboard();
        }

        private void BtnApartments_Click(object sender, RoutedEventArgs e)
        {
            new ApartmentsWindow(CurrentUserRole, CurrentOwnerId).ShowDialog();
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно отчетов и передаем роль
            ReportsWindow reportsWin = new ReportsWindow(CurrentUserRole, CurrentOwnerId);
            reportsWin.ShowDialog();
        }
        private void BtnTariffs_Click(object sender, RoutedEventArgs e)
        {
            TariffsWindow win = new TariffsWindow();
            win.ShowDialog();
        }

        // Обработчик кнопки:
        private void BtnUsers_Click(object sender, RoutedEventArgs e)
        {
            UsersWindow win = new UsersWindow();
            win.ShowDialog();
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            // Передаем ID текущего пользователя, чтобы открыть его профиль
            UserEditWindow win = new UserEditWindow(CurrentUserId);
            win.ShowDialog();
        }
    }
}