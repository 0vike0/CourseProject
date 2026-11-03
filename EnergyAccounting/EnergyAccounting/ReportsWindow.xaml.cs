using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using EnergyAccounting.Helpers;

namespace EnergyAccounting
{
    public partial class ReportsWindow : Window
    {
        private string _role;
        private int? _ownerId;
        private DataTable _currentData; // Храним текущие данные для экспорта

        public ReportsWindow(string role, int? ownerId)
        {
            InitializeComponent();
            _role = role;
            _ownerId = ownerId;

            if (_role == "Owner")
            {
                TxtTitle.Text = "📈 Мои начисления";
            }

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string query = "";
                SqlParameter[] parameters = null;

                // Формируем запрос в зависимости от роли
                if (_role == "Owner")
                {
                    query = @"
                        SELECT 
                            a.Number AS [Квартира],
                            r.Name AS [Ресурс],
                            rd.ReadingDate AS [Дата],
                            rd.Value AS [Показание],
                            ISNULL(rd.Value - LAG(rd.Value) OVER(PARTITION BY m.id ORDER BY rd.ReadingDate), 0) AS [Расход],
                            t.Price AS [Тариф],
                            (ISNULL(rd.Value - LAG(rd.Value) OVER(PARTITION BY m.id ORDER BY rd.ReadingDate), 0)) * t.Price AS [К оплате]
                        FROM [05.02_Reading] rd
                        JOIN [05.02_Meter] m ON rd.MeterId = m.id
                        JOIN [05.02_Apartment] a ON m.ApartmentId = a.id
                        JOIN [05.02_Resource] r ON m.ResourceId = r.id
                        CROSS APPLY (
                            SELECT TOP 1 Price FROM [05.02_Tariff] 
                            WHERE ResourceId = r.id AND EffectiveDate <= rd.ReadingDate 
                            ORDER BY EffectiveDate DESC
                        ) t
                        WHERE a.OwnerId = @OwnerId
                        ORDER BY rd.ReadingDate DESC";
                    parameters = new SqlParameter[] { new SqlParameter("@OwnerId", _ownerId) };
                }
                else
                {
                    query = @"
                        SELECT 
                            a.Number AS [Квартира],
                            o.FullName AS [Собственник],
                            r.Name AS [Ресурс],
                            rd.ReadingDate AS [Дата],
                            rd.Value AS [Показание],
                            ISNULL(rd.Value - LAG(rd.Value) OVER(PARTITION BY m.id ORDER BY rd.ReadingDate), 0) AS [Расход],
                            t.Price AS [Тариф],
                            (ISNULL(rd.Value - LAG(rd.Value) OVER(PARTITION BY m.id ORDER BY rd.ReadingDate), 0)) * t.Price AS [Сумма]
                        FROM [05.02_Reading] rd
                        JOIN [05.02_Meter] m ON rd.MeterId = m.id
                        JOIN [05.02_Apartment] a ON m.ApartmentId = a.id
                        JOIN [05.02_Owner] o ON a.OwnerId = o.id
                        JOIN [05.02_Resource] r ON m.ResourceId = r.id
                        CROSS APPLY (
                            SELECT TOP 1 Price FROM [05.02_Tariff] 
                            WHERE ResourceId = r.id AND EffectiveDate <= rd.ReadingDate 
                            ORDER BY EffectiveDate DESC
                        ) t
                        ORDER BY rd.ReadingDate DESC";
                }

                _currentData = DatabaseHelper.ExecuteQuery(query, parameters);
                GridReports.ItemsSource = _currentData.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчетов: " + ex.Message);
            }
        }

        private void CmbPeriod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridReports != null) LoadData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ==========================================
        // ЛОГИКА ЭКСПОРТА В EXCEL (БЕЗ ВНЕШНИХ БИБЛИОТЕК)
        // ==========================================
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentData == null || _currentData.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта. Сначала сформируйте отчет.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xls)|*.xls|All Files (*.*)|*.*",
                FileName = $"Отчет_Энергоресурсы_{DateTime.Now:yyyy-MM-dd}.xls",
                Title = "Сохранить отчет в Excel"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:x='urn:schemas-microsoft-com:office:excel' xmlns='http://www.w3.org/TR/REC-html40'>");
                    sb.Append("<head>");
                    sb.Append("<meta charset='utf-8'>");
                    // Настройка формата страницы (альбомная ориентация)
                    sb.Append("<style> table { border-collapse: collapse; } th, td { border: 1px solid #E1BEE7; padding: 5px; font-family: Arial; font-size: 12px; } ");
                    sb.Append("th { background-color: #E1BEE7; color: #4A148C; font-weight: bold; }</style>");
                    sb.Append("</head>");
                    sb.Append("<body>");

                    // Заголовок отчета
                    sb.Append($"<h2>Отчет по энергоресурсам от {DateTime.Now:dd.MM.yyyy}</h2>");

                    sb.Append("<table>");

                    // Записываем шапку таблицы
                    sb.Append("<tr>");
                    foreach (DataColumn column in _currentData.Columns)
                    {
                        sb.Append($"<th>{column.ColumnName}</th>");
                    }
                    sb.Append("</tr>");

                    // Записываем данные
                    foreach (DataRow row in _currentData.Rows)
                    {
                        sb.Append("<tr>");
                        foreach (var item in row.ItemArray)
                        {
                            // Форматируем даты и числа
                            if (item is DateTime dt)
                                sb.Append($"<td>{dt:dd.MM.yyyy}</td>");
                            else if (item is double || item is decimal || item is int)
                                sb.Append($"<td style='mso-number-format:\"0.00\"'>{item}</td>");
                            else
                                sb.Append($"<td>{item}</td>");
                        }
                        sb.Append("</tr>");
                    }

                    sb.Append("</table>");
                    sb.Append("</body>");
                    sb.Append("</html>");

                    // Сохраняем файл в кодировке UTF-8
                    System.IO.File.WriteAllText(saveFileDialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);

                    MessageBox.Show("Отчет успешно выгружен в Excel!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте в Excel: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}