using Microsoft.Data.Sqlite;
using SQLite;
using System.Diagnostics;
using VinhKhanhTour.Models;

namespace VinhKhanhTour.Data
{
    public class PoiRepository
    {
        private SQLiteAsyncConnection? _connection;
        private readonly Services.IErrorHandler _errorHandler;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _hasInitialized = false;

        public PoiRepository(Services.IErrorHandler errorHandler)
        {
            _errorHandler = errorHandler;
        }

        private async Task InitAsync()
        {
            if (_hasInitialized && _connection is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_hasInitialized && _connection is not null) return;

                // Bug#1 Fix: Không xóa DB nữa → dữ liệu tồn tại giữa các lần mở app
                var dbPath = Constants.DatabasePath.Replace("Data Source=", "");

                _connection = new SQLiteAsyncConnection(dbPath);
                await _connection.CreateTableAsync<Poi>();
                // Không seed data — POI chỉ lấy từ CMS API

                _hasInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<List<Poi>> GetAllPoisAsync()
        {
            try
            {
                await InitAsync();
                return await _connection!.Table<Poi>().ToListAsync();
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                Debug.WriteLine($"Failed to retrieve POIs: {ex.Message}");
            }

            return new List<Poi>();
        }

        public async Task<int> SavePoiAsync(Poi poi)
        {
            try
            {
                await InitAsync();
                if (poi.Id != 0)
                {
                    return await _connection!.UpdateAsync(poi);
                }
                else
                {
                    return await _connection!.InsertAsync(poi);
                }
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                Debug.WriteLine($"Failed to save POI: {ex.Message}");
            }

            return 0;
        }

        public async Task<int> DeletePoiAsync(Poi poi)
        {
            try
            {
                await InitAsync();
                return await _connection!.DeleteAsync(poi);
            }
            catch (Exception ex)
            {
                _errorHandler.HandleError(ex);
                Debug.WriteLine($"Failed to delete POI: {ex.Message}");
            }

            return 0;
        }
    }
}
