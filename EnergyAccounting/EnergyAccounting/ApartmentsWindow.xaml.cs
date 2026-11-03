using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class ApartmentsWindow : Window
    {
        private string _role;
        private int? _ownerId;

        public ApartmentsWindow(string role, int? ownerId)
        {
            InitializeComponent();
            _role = role;
            _ownerId = ownerId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string query = @"
                    SELECT 
                        a.Number AS [Номер квартиры], 
                        a.Area AS [Площадь (кв.м.)], 
                        o.FullName AS [Собственник], 
                        o.Phone AS [Телефон]
                    FROM [05.02_Apartment] a
                    JOIN [05.02_Owner] o ON a.OwnerId = o.id
                    ORDER BY a.Number";

                // Если это собственник, показываем только его квартиры
                if (_role == "Owner" && _ownerId.HasValue)
                {
                    query = @"
                        SELECT 
                            a.Number AS [Номер квартиры], 
                            a.Area AS [Площадь (кв.м.)], 
                            o.FullName AS [Собственник], 
                            o.Phone AS [Телефон]
                        FROM [05.02_Apartment] a
                        JOIN [05.02_Owner] o ON a.OwnerId = o.id
                        WHERE a.OwnerId = @OwnerId
                        ORDER BY a.Number";
                }

                SqlParameter[] p = (_role == "Owner" && _ownerId.HasValue)
                    ? new SqlParameter[] { new SqlParameter("@OwnerId", _ownerId.Value) }
                    : null;

                DataTable dt = DatabaseHelper.ExecuteQuery(query, p);
                GridApartments.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}