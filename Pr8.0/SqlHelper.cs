using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

public partial class SqlHelper
{
    
    public void ExecuteNonQuery(string query, SqlConnection connection, SqlTransaction transaction, params SqlParameter[] parameters)
    {
        if (connection == null || transaction == null)
        {
            throw new InvalidOperationException("SqlHelper.ExecuteNonQuery: Connection and Transaction must be provided.");
        }

        using (var command = new SqlCommand(query, connection, transaction))
        {
            command.Parameters.AddRange(parameters);
            command.ExecuteNonQuery();
        }
    }

    // Выполнение команды и получение одного значения (scalar)
    public object ExecuteScalar(string query, SqlConnection connection, SqlTransaction transaction, params SqlParameter[] parameters)
    {
        if (connection == null || transaction == null)
        {
            throw new InvalidOperationException("SqlHelper.ExecuteScalar: Connection and Transaction must be provided.");
        }

        using (var command = new SqlCommand(query, connection, transaction))
        {
            command.Parameters.AddRange(parameters);
            return command.ExecuteScalar();
        }
    }

    // Выполнение команды и получение данных в виде DataTable
    public DataTable ExecuteDataTable(string query, SqlConnection connection, SqlTransaction transaction, params SqlParameter[] parameters)
    {
        if (connection == null || transaction == null)
        {
            throw new InvalidOperationException("SqlHelper.ExecuteDataTable: Connection and Transaction must be provided.");
        }

        DataTable dt = new DataTable();
        using (var command = new SqlCommand(query, connection, transaction))
        {
            command.Parameters.AddRange(parameters);
            using (var adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(dt);
            }
        }
        return dt;
    }

    // --- Методы для выполнения команд БЕЗ транзакции ---
    // Эти методы управляют собственным соединением и закрывают его.

    public void ExecuteNonQuery(string query, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(this.connectionString)) // Нужна строка подключения
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    public object ExecuteScalar(string query, params SqlParameter[] parameters)
    {
        using (var connection = new SqlConnection(this.connectionString)) // Нужна строка подключения
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }
    }

    public DataTable ExecuteDataTable(string query, params SqlParameter[] parameters)
    {
        DataTable dt = new DataTable();
        using (var connection = new SqlConnection(this.connectionString)) // Нужна строка подключения
        {
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dt);
                }
            }
        }
        return dt;
    }

    // --- Получение строки подключения (для создания соединения вне SqlHelper) ---
    // Добавим поле для строки подключения
    private readonly string connectionString;
    public string GetConnectionString() { return connectionString; }

    // Конструктор теперь принимает строку подключения
    public SqlHelper(string connectionString)
    {
        this.connectionString = connectionString;
    }


    // --- Метод для создания параметров ---
    public static SqlParameter CreateParameter(string name, object value, SqlDbType dbType)
    {
        return new SqlParameter(name, dbType) { Value = value ?? DBNull.Value };
    }
}