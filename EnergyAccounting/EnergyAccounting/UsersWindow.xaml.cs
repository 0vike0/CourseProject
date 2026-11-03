using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class UsersWindow : Window
    {
        public UsersWindow()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                string query = @"
                    SELECT u.id, u.Login, ISNULL(o.FullName, 'Не указано') AS FullName, u.Role, 
                           ISNULL(u.FailedAttempts, 0) AS Attempts, u.IsBlocked, u.AvatarPath
                    FROM [05.02_User] u
                    LEFT JOIN [05.02_Owner] o ON u.OwnerId = o.id";
                
                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                var list = new System.Collections.Generic.List<UserDisplay>();
                foreach (DataRow row in dt.Rows)
                {
                    string avatarPath = row["AvatarPath"] != DBNull.Value ? row["AvatarPath"].ToString() : "";
                    
                    // Проверка на существование файла. Если нет — ставим заглушку
                    if (string.IsNullOrEmpty(avatarPath) || !File.Exists(avatarPath))
                    {
                        avatarPath = "pack://siteoforigin:,,,/Avatars/ava.png";
                        if (!File.Exists("Avatars/ava.png")) // Если и заглушки нет, ставим дефолтную системную
                            avatarPath = "pack://application:,,,/Avatars/ava.png"; 
                    }

                    list.Add(new UserDisplay
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Login = row["Login"].ToString(),
                        FullName = row["FullName"].ToString(),
                        Role = row["Role"].ToString(),
                        Attempts = row["Attempts"].ToString(),
                        IsBlockedText = Convert.ToBoolean(row["IsBlocked"]) ? "Да" : "Нет",
                        AvatarPath = avatarPath
                    });
                }
                GridUsers.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void BtnUnblock_Click(object sender, RoutedEventArgs e)
        {
            if (GridUsers.SelectedItem is UserDisplay selected)
            {
                string query = "UPDATE [05.02_User] SET IsBlocked = 0, FailedAttempts = 0 WHERE id = @Id";
                DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@Id", selected.Id) });
                MessageBox.Show("Пользователь разблокирован!");
                LoadUsers();
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (GridUsers.SelectedItem is UserDisplay selected)
            {
                if (MessageBox.Show($"Удалить пользователя {selected.Login}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    string query = "DELETE FROM [05.02_User] WHERE id = @Id";
                    DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[] { new SqlParameter("@Id", selected.Id) });
                    LoadUsers();
                }
            }
        }

        // Для добавления и редактирования мы будем использовать одно окно, но передавать разные данные
        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            UserEditWindow win = new UserEditWindow(null, true);
            win.ShowDialog();
            LoadUsers();
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (GridUsers.SelectedItem is UserDisplay selected)
            {
                UserEditWindow win = new UserEditWindow(selected.Id, true);
                win.ShowDialog();
                LoadUsers();
            }
        }
    }

    // Класс для привязки к DataGrid
    public class UserDisplay
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Attempts { get; set; }
        public string IsBlockedText { get; set; }
        public string AvatarPath { get; set; }
    }
}