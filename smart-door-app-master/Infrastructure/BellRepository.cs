using System.Data.Common;
using Common;
using Common.Models;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure;

public class BellRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<BellRepository> _logger;

    public BellRepository(NpgsqlDataSource dataSource, ILogger<BellRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }
    
    public async Task<BellData> AddBellData(string fromTopic, string toTopic, string message)
    {
        const string query = @"
            INSERT INTO smart_door_bell.bell_data (from_topic, to_topic, message, message_at) 
            VALUES (@FromTopic, @ToTopic, @Message, @MessageAt) 
            RETURNING *;";
        try
        {
            using (var connection = _dataSource.CreateConnection())
            {
                _logger.LogInformation("Adding bell data...");
                return await connection.QueryFirstAsync<BellData>(query, new
                {
                    FromTopic = fromTopic,
                    ToTopic = toTopic,
                    Message = message,
                    MessageAt = DateTime.UtcNow // Use DateTime.UtcNow instead of string
                });
            }
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "A database error occurred while adding bell data.");
            throw new AppException("A database error occurred while adding bell data. Please try again later.");
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "A database error occurred while adding bell data.");
            throw new AppException("A database error occurred while adding bell data. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding bell data.");
            throw new AppException("An unexpected error occurred while adding bell data. Please try again later.");
        }
    }
    
    public async Task<IEnumerable<BellData>> GetBellData()
    {
        const string query = @$"SELECT from_topic as {nameof(BellData.FromTopic)},
             to_topic as {nameof(BellData.ToTopic)}, message as {nameof(BellData.Message)}, 
             message_at as {nameof(BellData.MessageAt)} FROM smart_door_bell.bell_data ORDER BY message_at DESC";
        try
        {
            using (var connection = _dataSource.CreateConnection())
            {
                _logger.LogInformation("Fetching bell log.");
                return await connection.QueryAsync<BellData>(query);
            }
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "A database error occurred while fetching bell data.");
            throw new AppException("A database error occurred while fetching bell data. Please try again later.");
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "A database error occurred while fetching bell data.");
            throw new AppException("A database error occurred while fetching bell data. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching bell data.");
            throw new AppException("An unexpected error occurred while fetching bell data. Please try again later.");
        }
    }
    
    public async Task DeleteWholeData()
    {
        const string query = "TRUNCATE TABLE smart_door_bell.bell_data";
        try
        {
            using (var connection = _dataSource.CreateConnection())
            {
                _logger.LogInformation("Deleting all bell data");
                await connection.ExecuteAsync(query);
            }
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "A database error occurred while deleting bell data.");
            throw new AppException("A database error occurred while deleting bell data. Please try again later.");
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "A database error occurred while deleting bell data.");
            throw new AppException("A database error occurred while deleting bell data. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting bell data.");
            throw new AppException("An unexpected error occurred while deleting bell data. Please try again later.");
        }
    }
    
    public async Task<IEnumerable<BellData>> GetBellDataByClosestTimeRange(string fromTopic, string toTopic, DateTime messageAt)
    { 
        const string query = $@"
SELECT from_topic as {nameof(BellData.FromTopic)}, 
               to_topic as {nameof(BellData.ToTopic)}, 
               message as {nameof(BellData.Message)}, 
               message_at as {nameof(BellData.MessageAt)} 
        FROM smart_door_bell.bell_data 
        WHERE from_topic = @FromTopic 
          AND to_topic = @ToTopic 
          AND message_at BETWEEN @StartTime AND @EndTime
        ORDER BY ABS(EXTRACT(EPOCH FROM (message_at - @MessageAt)))
        LIMIT 10;";

        try
        {
            using (var connection = _dataSource.CreateConnection())
            {
                _logger.LogInformation("Fetching list of bell data by closest time range.");
                return await connection.QueryAsync<BellData>(query, new
                {
                    FromTopic = fromTopic,
                    ToTopic = toTopic,
                    StartTime = messageAt.AddMinutes(-10),
                    EndTime = messageAt.AddMinutes(10),
                    MessageAt = messageAt
                });
            }
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, "A database error occurred while fetching bell data by closest time range.");
            throw new AppException("A database error occurred while fetching bell data by closest time range. Please try again later.");
        }
        catch (DbException ex)
        {
            _logger.LogError(ex, "A database error occurred while fetching bell data by closest time range.");
            throw new AppException("A database error occurred while fetching bell data by closest time range. Please try again later.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching bell data by closest time range.");
            throw new AppException("An unexpected error occurred while fetching bell data by closest time range. Please try again later.");
        }
    }
}
