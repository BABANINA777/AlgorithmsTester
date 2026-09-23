using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
namespace AlgorithmsTester.Core;
public class DatabaseManager
{
    // Метод, который возвращает строку подключения к файлу benchmark.db
    public static string GetConnectionString()
    {
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "benchmark.db");

        // Если запускаем консоль из AlgorithmsTester.Core
        string coreProjectDb = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\benchmark.db"));

        // Если запускаем GUI из AlgorithmsTester.Desktop
        string desktopToCoreDb = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\AlgorithmsTester.Core\benchmark.db"));

        if (File.Exists(coreProjectDb))
        {
            dbPath = coreProjectDb;
        }
        else if (File.Exists(desktopToCoreDb))
        {
            dbPath = desktopToCoreDb;
        }

        SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
        builder.DataSource = dbPath;

        return builder.ConnectionString;
    }

    // ВОТ САМ МЕТОД: принимает параметры эксперимента и массив результатов
    public static long SaveExperimentRun(
        string groupName,
        IAlgorithmTemplate algorithm,
        int nStart,
        int nStop,
        int step,
        int repeats,
        List<double> results)
    {
        string connectionString = GetConnectionString();
        long experimentId = 0;

        using (SqliteConnection connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            // Открываем транзакцию, чтобы 2000 записей влетели в базу за 0.05 секунды
            using (SqliteTransaction transaction = connection.BeginTransaction())
            {
                // 1. Записываем сам эксперимент в таблицу Experiments
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO Experiments (GroupName, AlgorithmName, ExperimentDate, NStart, NStop, Step, RunsCount)
                        VALUES (@group, @algo, @date, @nStart, @nStop, @step, @runs);
                        SELECT last_insert_rowid();";
                    command.Parameters.AddWithValue("@group", groupName);
                    command.Parameters.AddWithValue("@algo", algorithm.Name);
                    command.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@nStart", nStart);
                    command.Parameters.AddWithValue("@nStop", nStop);
                    command.Parameters.AddWithValue("@step", step);
                    command.Parameters.AddWithValue("@runs", repeats);
                    // Выполняем и забираем ID созданной строки
                    object result = command.ExecuteScalar();
                    experimentId = Convert.ToInt64(result);
                }
                // 2. В цикле записываем все точки замеров в таблицу Measurements
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = @"
                        INSERT INTO Measurements (ExperimentId, N, RunNumber, ElapsedTimeMs)
                        VALUES (@expId, @n, 1, @time);";
                    // Создаем параметры один раз перед циклом
                    command.Parameters.Add("@expId", SqliteType.Integer);
                    command.Parameters.Add("@n", SqliteType.Integer);
                    command.Parameters.Add("@time", SqliteType.Real);
                    command.Parameters["@expId"].Value = experimentId;
                    int currentN = nStart;
                    for (int i = 0; i < results.Count; i++)
                    {
                        command.Parameters["@n"].Value = currentN;
                        command.Parameters["@time"].Value = results[i];
                        command.ExecuteNonQuery();
                        currentN = currentN + step;
                    }
                }
                // Фиксируем всё в файле базы данных
                transaction.Commit();
            }
        }

        return experimentId;
    }

    // Получение списка всех сохраненных прогонов для конкретной группы (карточки)
    public static List<RunHistoryItem> GetHistoryRuns(string groupName)
    {
        List<RunHistoryItem> list = new List<RunHistoryItem>();
        string connectionString = GetConnectionString();

        using (SqliteConnection connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT Id, GroupName, AlgorithmName, ExperimentDate, NStart, NStop, Step, RunsCount
                    FROM Experiments
                    WHERE GroupName = @group 
                       OR @group LIKE '%' || GroupName || '%' 
                       OR GroupName LIKE '%' || @group || '%'
                    ORDER BY Id DESC;";

                command.Parameters.AddWithValue("@group", groupName);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        RunHistoryItem item = new RunHistoryItem();
                        item.Id = reader.GetInt64(0);
                        item.GroupName = reader.GetString(1);
                        item.AlgorithmName = reader.GetString(2);
                        item.ExperimentDate = reader.GetString(3);
                        item.NStart = reader.GetInt32(4);
                        item.NStop = reader.GetInt32(5);
                        item.Step = reader.GetInt32(6);
                        item.RunsCount = reader.GetInt32(7);

                        list.Add(item);
                    }
                }
            }
        }

        return list;
    }

    // Получение координат (N и время) для построения графика по ID эксперимента
    public static (double[] xs, double[] ys) GetRunPlotData(long experimentId)
    {
        List<double> xsList = new List<double>();
        List<double> ysList = new List<double>();
        string connectionString = GetConnectionString();

        using (SqliteConnection connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT N, ElapsedTimeMs
                    FROM Measurements
                    WHERE ExperimentId = @expId
                    ORDER BY N ASC;";

                command.Parameters.AddWithValue("@expId", experimentId);

                using (SqliteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double n = Convert.ToDouble(reader.GetInt32(0));
                        double time = reader.GetDouble(1);

                        xsList.Add(n);
                        ysList.Add(time);
                    }
                }
            }
        }

        return (xsList.ToArray(), ysList.ToArray());
    }
}