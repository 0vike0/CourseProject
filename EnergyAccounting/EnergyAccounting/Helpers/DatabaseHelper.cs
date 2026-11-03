using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;


namespace EnergyAccounting.Helpers
{
    public static class DatabaseHelper
    {
        // Берем строку подключения из App.config
        // Убираем зависимость от App.config
        private static readonly string connectionString = "Server=stud-mssql.sttec.yar.ru,38325;Database=user105_db;User Id=user105_db;Password=user105;TrustServerCertificate=True;";
        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        // ВАЖНО: Создаем КОПИИ параметров, чтобы не было конфликта
                        foreach (var p in parameters)
                        {
                            command.Parameters.Add(new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value));
                        }
                    }

                    DataTable dataTable = new DataTable();
                    try
                    {
                        connection.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Ошибка при выполнении запроса: " + ex.Message);
                    }
                    return dataTable;
                }
            }
        }

        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        // ВАЖНО: Создаем КОПИИ параметров, чтобы не было конфликта
                        foreach (var p in parameters)
                        {
                            command.Parameters.Add(new SqlParameter(p.ParameterName, p.Value ?? DBNull.Value));
                        }
                    }

                    try
                    {
                        connection.Open();
                        return command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Ошибка при выполнении команды: " + ex.Message);
                    }
                }
            }
        }
    }
}