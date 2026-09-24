using System.Collections.Generic;
using AlgorithmsTester.Core.Algorithm;
using Microsoft.Data.Sqlite;

namespace AlgorithmsTester.Core;

public class Programm
{
    public static void Main(string[] args)
    {
        int nstart = 80000;
        int nstop = 100000;
        int steps = 50;
        int repeats = 1;
        //var algorithm = new VectorAlgorithms();
        //var algorithm = new ConstAlgorithms();
        var algorithm = new MultiplicationAlgorithms();
        //var algorithm = new NaivePolynomialAlgorithm();
        //var algorithm = new HornerPolynomialAlgorithm();
        
        //var algorithm = new SimplePowAlgorithm();
        //var algorithm = new RecursiveLinearPowAlgorithm();
        //var algorithm = new QuickPowAlgorithm();
        
        //var algorithm = new DijkstraAlgorithm();
        //var algorithm = new KaratsubaAlgorithm();
        //var algorithm = new BubbleSortAlgorithm();
        //var algorithm = new QuickSortAlgorithm();
        //var algorithm = new TimsortAlgorithm();
        //var algorithm = new MatrixMultiplicationAlgorithm();
        
        var benchmark = new BenchmarkEngine(nstart, nstop,steps, repeats, algorithm);
        List<double> realtime = benchmark.AlgorithmTimer();
        //List<double> stepcount = benchmark.AlgorithmCount();
        TheoreticalFitter fitter = new TheoreticalFitter(algorithm);
        fitter.TeoreticalAlgorithmTimer(nstart, nstop, realtime, steps);
        //fitter.TeoreticalAlgorithmTimer(nstart, nstop, stepcount, steps);
        
        //DatabaseManager.SaveExperimentRun("Вектор", algorithm,nstart, nstop, steps, repeats, realtime);
        //CreateTable();
    }

    public static async void CreateTable()
    {
        // 1. Формируем строку подключения (файл создастся рядом с .exe)
        const string connectionString = "Data Source=alghoritm.db";

// await using гарантирует автоматическое закрытие файла даже при ошибке!
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

// 2. Создаем команду через само соединение
        await using var command = connection.CreateCommand();

// 3. Пишем SQL для создания таблицы пользователей
        command.CommandText = 
            """
            -- 1. Таблица сессий/экспериментов
            CREATE TABLE IF NOT EXISTS Experiments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- Уникальный номер прогона
                AlgorithmName TEXT NOT NULL,           -- Имя алгоритма (например, "Vector", "Karatsuba")
                GroupName TEXT NOT NULL,               -- группа алгоритма
                ExperimentDate TEXT NOT NULL,          -- Дата и время запуска
                NStart INTEGER NOT NULL,               -- С какого N начали
                NStop INTEGER NOT NULL,                -- До какого N считали
                Step INTEGER NOT NULL,                 -- Шаг прироста N
                RunsCount INTEGER NOT NULL DEFAULT 5   -- Число повторов для усреднения
            );
            
            -- 2. Таблица точек замеров
            CREATE TABLE IF NOT EXISTS Measurements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,  -- Уникальный номер замера
                ExperimentId INTEGER NOT NULL,         -- Ссылка на ID из таблицы Experiments
                N INTEGER NOT NULL,                    -- Размерность N
                RunNumber INTEGER NOT NULL,            -- Номер прогона (1, 2, 3, 4, 5)
                ElapsedTimeMs REAL NOT NULL,           -- Затраченное время (double в мс)
                StepCount INTEGER,                     -- Число шагов (для степеней, иначе NULL)
                
                -- Связываем замер с экспериментом (если удалить эксперимент — замеры удалятся сами)
                FOREIGN KEY (ExperimentId) REFERENCES Experiments(Id) ON DELETE CASCADE
            );
            
            -- Индекс для мгновенного поиска точек по номеру эксперимента
            CREATE INDEX IF NOT EXISTS idx_measurements_exp ON Measurements(ExperimentId);
            
            """;

// 4. Нажимаем кнопку ExecuteNonQuery: создаем таблицу, назад ничего не ждём
        await command.ExecuteNonQueryAsync();
    }
}