using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EnergyAccounting.Helpers;
using Microsoft.Win32;

namespace EnergyAccounting
{
    public partial class UserEditWindow : Window
    {
        private int? _userId;
        private bool _isAdmin;
        private string _selectedAvatarPath = "";

        // ИСПРАВЛЕНО: siteoforigin (добавлена буква 'f')
        private string _defaultAvatar = "pack://siteoforigin:,,,/Avatars/ava.png";

        public UserEditWindow(int? userId, bool isAdmin = false)
        {
            InitializeComponent();
            _userId = userId;
            _isAdmin = isAdmin;

            if (_userId.HasValue)
            {
                TxtTitle.Text = isAdmin ? "Редактирование пользователя" : "Мой профиль";
                LoadUserData();
            }
            else
            {
                TxtTitle.Text = "Новый пользователь";
                SetDefaultAvatar();
            }

            if (!_isAdmin)
            {
                CmbRole.IsEnabled = false;
                TxtLogin.IsEnabled = false;
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                // Пытаемся загрузить картинку-заглушку
                ImgAvatar.Source = new BitmapImage(new Uri(_defaultAvatar, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // Если картинки нет, программа просто не вылетит
            }
        }

        private void LoadUserData()
        {
            string query = @"
                SELECT u.Login, u.Password, u.Role, u.AvatarPath, u.OwnerId, o.FullName 
                FROM [05.02_User] u
                LEFT JOIN [05.02_Owner] o ON u.OwnerId = o.id
                WHERE u.id = @Id";

            DataTable dt = DatabaseHelper.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@Id", _userId) });

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                TxtLogin.Text = row["Login"].ToString();
                TxtPassword.Text = row["Password"].ToString();
                CmbRole.Text = row["Role"].ToString();

                if (row["FullName"] != DBNull.Value)
                    TxtFullName.Text = row["FullName"].ToString();

                string avatar = row["AvatarPath"] != DBNull.Value ? row["AvatarPath"].ToString() : "";

                if (!string.IsNullOrEmpty(avatar) && File.Exists(avatar))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(avatar, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        ImgAvatar.Source = bitmap;
                        _selectedAvatarPath = avatar;
                    }
                    catch
                    {
                        SetDefaultAvatar();
                    }
                }
                else
                {
                    SetDefaultAvatar();
                }
            }
        }

        private void BtnUploadAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите фотографию для аватара"
            };

            if (ofd.ShowDialog() == true)
            {
                string extension = Path.GetExtension(ofd.FileName).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".bmp")
                {
                    MessageBox.Show("Пожалуйста, выберите только файлы изображений (JPG, JPEG, PNG или BMP)!",
                                    "Неверный формат файла",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    string avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserAvatars");
                    if (!Directory.Exists(avatarsDir))
                        Directory.CreateDirectory(avatarsDir);

                    string newPath = Path.Combine(avatarsDir, $"{Guid.NewGuid()}{extension}");
                    File.Copy(ofd.FileName, newPath, true);

                    _selectedAvatarPath = newPath;

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(newPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImgAvatar.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось загрузить изображение. Возможно, файл поврежден.\n\nДетали: " + ex.Message,
                                    "Ошибка загрузки",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(TxtLogin.Text) || string.IsNullOrWhiteSpace(TxtPassword.Text))
            {
                MessageBox.Show("Логин и пароль не могут быть пустыми!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string role = ((ComboBoxItem)CmbRole.SelectedItem).Content.ToString();

                // 1. ПРОВЕРКА: существует ли уже пользователь с таким логином?
                string checkQuery = "SELECT COUNT(*) FROM [05.02_User] WHERE Login = @Login AND id != @Id";
                SqlParameter[] checkParams = {
            new SqlParameter("@Login", TxtLogin.Text),
            new SqlParameter("@Id", _userId ?? 0) // Если добавляем нового, Id = 0, значит проверятся все
        };

                DataTable checkResult = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);
                int existingCount = Convert.ToInt32(checkResult.Rows[0][0]);

                if (existingCount > 0)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует. Придумайте другой логин.",
                                    "Ошибка",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    return; // Прерываем сохранение
                }

                // 2. СОХРАНЕНИЕ
                if (_userId.HasValue)
                {
                    // Обновление
                    string query = @"
                UPDATE [05.02_User] 
                SET Login = @Login, Password = @Password, Role = @Role, AvatarPath = @Avatar 
                WHERE id = @Id";

                    DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[]
                    {
                new SqlParameter("@Login", TxtLogin.Text),
                new SqlParameter("@Password", TxtPassword.Text),
                new SqlParameter("@Role", role),
                new SqlParameter("@Avatar", string.IsNullOrEmpty(_selectedAvatarPath) ? DBNull.Value : (object)_selectedAvatarPath),
                new SqlParameter("@Id", _userId)
                    });

                    // Обновление ФИО в таблице собственников
                    if (!string.IsNullOrWhiteSpace(TxtFullName.Text))
                    {
                        string ownerQuery = @"
                    UPDATE o SET o.FullName = @FullName
                    FROM [05.02_Owner] o
                    JOIN [05.02_User] u ON u.OwnerId = o.id
                    WHERE u.id = @Id";
                        DatabaseHelper.ExecuteNonQuery(ownerQuery, new SqlParameter[]
                        {
                    new SqlParameter("@FullName", TxtFullName.Text),
                    new SqlParameter("@Id", _userId)
                        });
                    }
                }
                else
                {
                    // Добавление нового
                    string query = "INSERT INTO [05.02_User] (Login, Password, Role, AvatarPath) VALUES (@Login, @Password, @Role, @Avatar)";
                    DatabaseHelper.ExecuteNonQuery(query, new SqlParameter[]
                    {
                new SqlParameter("@Login", TxtLogin.Text),
                new SqlParameter("@Password", TxtPassword.Text),
                new SqlParameter("@Role", role),
                new SqlParameter("@Avatar", string.IsNullOrEmpty(_selectedAvatarPath) ? DBNull.Value : (object)_selectedAvatarPath)
                    });
                }

                this.Close(); // Закрываем окно при успехе
            }
            catch (Exception ex)
            {
                // На случай других непредвиденных ошибок (например, нет связи с БД)
                MessageBox.Show("Произошла ошибка при сохранении: " + ex.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}