using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class TariffsWindow : Window
    {
        public TariffsWindow()
        {
            InitializeComponent();
            LoadResources();
            LoadTariffs();
        }

        // Загрузка списка ресурсов в ComboBox
        private void LoadResources()
        {
            try
            {
                DataTable dt = DatabaseHelper.ExecuteQuery("SELECT id, Name FROM [05.02_Resource]");
                CmbResource.ItemsSource = dt.DefaultView;
                CmbResource.SelectedIndex = 0; // Выбираем первый по умолчанию
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ресурсов: " + ex.Message);
            }
        }

        // Загрузка существующих тарифов в таблицу
        private void LoadTariffs()
        {
            try
            {
                string query = @"
                    SELECT t.id, r.Name as ResourceName, t.Price, t.EffectiveDate 
                    FROM [05.02_Tariff] t
                    JOIN [05.02_Resource] r ON t.ResourceId = r.id
                    ORDER BY t.EffectiveDate DESC";

                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                // Преобразуем в список для красивого отображения
                var list = new System.Collections.Generic.List<object>();
                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new
                    {
                        ResourceName = row["ResourceName"].ToString(),
                        Price = Convert.ToDecimal(row["Price"]).ToString("F2"),
                        EffectiveDate = Convert.ToDateTime(row["EffectiveDate"]).ToString("dd.MM.yyyy")
                    });
                }
                GridTariffs.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки тарифов: " + ex.Message);
            }
        }

        // Добавление нового тарифа
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CmbResource.SelectedItem == null || string.IsNullOrWhiteSpace(TxtPrice.Text) || DpDate.SelectedDate == null)
            {
                MessageBox.Show("Заполните все поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtPrice.Text.Replace('.', ','), out decimal price) || price <= 0) // Добавили замену точки на запятую
            {
                MessageBox.Show("Введите корректную цену (больше нуля)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Получаем DataRowView, который выбран в ComboBox
                DataRowView selectedRow = (DataRowView)CmbResource.SelectedItem;
                int resourceId = Convert.ToInt32(selectedRow["id"]);
                DateTime effectiveDate = DpDate.SelectedDate.Value;

                string query = @"
            INSERT INTO [05.02_Tariff] (Price, EffectiveDate, ResourceId) 
            VALUES (@Price, @Date, @ResourceId)";

                SqlParameter[] p =
                {
            new SqlParameter("@Price", price),
            new SqlParameter("@Date", effectiveDate),
            new SqlParameter("@ResourceId", resourceId)
        };

                DatabaseHelper.ExecuteNonQuery(query, p);

                MessageBox.Show("Тариф успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                TxtPrice.Clear();
                DpDate.SelectedDate = null;
                LoadTariffs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}