using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    // Класс для строки таблицы
    public class ReadingRow : System.ComponentModel.INotifyPropertyChanged
    {
        private decimal? _currentValue;
        public int MeterId { get; set; }
        public string ResourceName { get; set; }
        public decimal PreviousValue { get; set; }

        public decimal? CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged(nameof(CurrentValue));
                OnPropertyChanged(nameof(Consumption));
            }
        }
        public decimal? Consumption => CurrentValue.HasValue ? CurrentValue.Value - PreviousValue : (decimal?)null;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public partial class ReadingsWindow : Window
    {
        private string _role;
        private int? _ownerId;

        public ReadingsWindow(string role, int? ownerId)
        {
            InitializeComponent();
            _role = role;
            _ownerId = ownerId;
            LoadApartments();
        }

        private void LoadApartments()
        {
            try
            {
                string query = @"
                    SELECT a.id, a.Number, o.FullName 
                    FROM [05.02_Apartment] a 
                    JOIN [05.02_Owner] o ON a.OwnerId = o.id";

                // Собственник видит только свои квартиры
                if (_role == "Owner" && _ownerId.HasValue)
                {
                    query += $" WHERE a.OwnerId = {_ownerId.Value}";
                }

                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                // Создаем красивый список для отображения
                List<ApartmentDisplay> list = new List<ApartmentDisplay>();
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new ApartmentDisplay
                    {
                        Id = Convert.ToInt32(row["id"]),
                        DisplayText = $"Кв. № {row["Number"]} ({row["FullName"]})"
                    });
                }

                ListApartments.ItemsSource = list;
                ListApartments.DisplayMemberPath = "DisplayText";

                // Выделяем первую квартиру
                if (list.Count > 0) ListApartments.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки квартир: " + ex.Message);
            }
        }

        private class ApartmentDisplay
        {
            public int Id { get; set; }
            public string DisplayText { get; set; }
        }

        private void ListApartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListApartments.SelectedItem is ApartmentDisplay selectedApt)
            {
                TxtApartmentInfo.Text = selectedApt.DisplayText;
                LoadReadingsForApartment(selectedApt.Id);
            }
        }

        private void LoadReadingsForApartment(int apartmentId)
        {
            try
            {
                // Получаем счётчики и последние показания для них
                string query = @"
                    SELECT m.id AS MeterId, r.Name AS ResourceName,
                           ISNULL((SELECT TOP 1 Value FROM [05.02_Reading] WHERE MeterId = m.id ORDER BY ReadingDate DESC), 0) AS PreviousValue
                    FROM [05.02_Meter] m
                    JOIN [05.02_Resource] r ON m.ResourceId = r.id
                    WHERE m.ApartmentId = @ApartmentId";

                SqlParameter[] p = { new SqlParameter("@ApartmentId", apartmentId) };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, p);

                List<ReadingRow> readings = new List<ReadingRow>();
                foreach (DataRow row in dt.Rows)
                {
                    readings.Add(new ReadingRow
                    {
                        MeterId = Convert.ToInt32(row["MeterId"]),
                        ResourceName = row["ResourceName"].ToString(),
                        PreviousValue = Convert.ToDecimal(row["PreviousValue"]),
                        CurrentValue = null
                    });
                }
                GridReadings.ItemsSource = readings;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки показаний: " + ex.Message);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (GridReadings.ItemsSource is List<ReadingRow> readings)
            {
                int savedCount = 0;
                int errorCount = 0;
                try
                {
                    foreach (var item in readings)
                    {
                        if (item.CurrentValue.HasValue)
                        {
                            // Проверка: новые показания не могут быть меньше предыдущих
                            if (item.CurrentValue <= item.PreviousValue)
                            {
                                errorCount++;
                                continue; // Пропускаем эту запись
                            }

                            string query = "INSERT INTO [05.02_Reading] (ReadingDate, Value, MeterId) VALUES (GETDATE(), @Value, @MeterId)";
                            SqlParameter[] p =
                            {
                                new SqlParameter("@Value", item.CurrentValue.Value),
                                new SqlParameter("@MeterId", item.MeterId)
                            };
                            DatabaseHelper.ExecuteNonQuery(query, p);
                            savedCount++;
                        }
                    }

                    if (errorCount > 0)
                        MessageBox.Show($"Сохранено: {savedCount}. Ошибок (показания меньше предыдущих): {errorCount}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else if (savedCount > 0)
                        MessageBox.Show($"Успешно сохранено {savedCount} показаний!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show("Вы не ввели ни одного нового показания.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Перезагружаем, чтобы обновить колонку "Предыдущее"
                    if (ListApartments.SelectedItem is ApartmentDisplay selectedApt)
                        LoadReadingsForApartment(selectedApt.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения: " + ex.Message);
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}