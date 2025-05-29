using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Models;

namespace PoultrySlaughterPOS.Services.Repositories
{
    /// <summary>
    /// TruckLoad repository implementation providing enterprise-level truck loading operations management
    /// supporting complex logistics workflows and performance optimization metrics
    /// FIXED: Complete implementation of all interface methods including GetLatestTruckLoadAsync
    /// </summary>
    public class TruckLoadRepository : Repository<TruckLoad>, ITruckLoadRepository
    {
        public TruckLoadRepository(PoultryDbContext context, ILogger<TruckLoadRepository> logger)
            : base(context, logger)
        {
        }

        #region Core Load Management Operations

        public async Task<TruckLoad?> GetTruckCurrentLoadAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateTime.Today;
                return await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate.Date == today)
                    .OrderByDescending(tl => tl.LoadDate)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current load for truck {TruckId}", truckId);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetTruckLoadsByDateAsync(DateTime loadDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = loadDate.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .OrderBy(tl => tl.TruckId)
                    .ThenBy(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving truck loads for date {LoadDate}", loadDate);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetTruckLoadsByDateRangeAsync(int truckId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var fromDate = startDate.Date;
                var toDate = endDate.Date.AddDays(1);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= fromDate && tl.LoadDate < toDate)
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving truck loads for truck {TruckId} from {StartDate} to {EndDate}",
                    truckId, startDate, endDate);
                throw;
            }
        }

        /// <summary>
        /// FIXED: Implementation of GetLatestTruckLoadAsync for POS integration
        /// Gets the most recent truck load for a specific truck (same as GetMostRecentLoadAsync)
        /// </summary>
        public async Task<TruckLoad?> GetLatestTruckLoadAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TruckId == truckId)
                    .OrderByDescending(tl => tl.LoadDate)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest load for truck {TruckId}", truckId);
                throw;
            }
        }

        public async Task<TruckLoad?> GetMostRecentLoadAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TruckId == truckId)
                    .OrderByDescending(tl => tl.LoadDate)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most recent load for truck {TruckId}", truckId);
                throw;
            }
        }

        #endregion

        #region Load Status Management and Workflow

        public async Task<IEnumerable<TruckLoad>> GetLoadsByStatusAsync(string status, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.Status == status)
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loads by status {Status}", status);
                throw;
            }
        }

        public async Task<bool> UpdateLoadStatusAsync(int loadId, string newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var load = await _dbSet
                    .FirstOrDefaultAsync(tl => tl.LoadId == loadId, cancellationToken)
                    .ConfigureAwait(false);

                if (load == null)
                {
                    _logger.LogWarning("Load {LoadId} not found for status update", loadId);
                    return false;
                }

                load.Status = newStatus;
                load.UpdatedDate = DateTime.Now;

                var rowsAffected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated status for load {LoadId} to {NewStatus}", loadId, newStatus);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for load {LoadId} to {NewStatus}", loadId, newStatus);
                throw;
            }
        }

        public async Task<int> UpdateMultipleLoadStatusAsync(IEnumerable<int> loadIds, string newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var loadIdsList = loadIds.ToList();
                var loads = await _dbSet
                    .Where(tl => loadIdsList.Contains(tl.LoadId))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var load in loads)
                {
                    load.Status = newStatus;
                    load.UpdatedDate = DateTime.Now;
                }

                var rowsAffected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated status for {Count} loads to {NewStatus}", loads.Count, newStatus);
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for multiple loads to {NewStatus}", newStatus);
                throw;
            }
        }

        public async Task<bool> IsLoadInProgressAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateTime.Today;
                return await _dbSet
                    .AnyAsync(tl => tl.TruckId == truckId &&
                                   tl.LoadDate.Date == today &&
                                   (tl.Status == "LOADED" || tl.Status == "IN_TRANSIT"),
                             cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if load is in progress for truck {TruckId}", truckId);
                throw;
            }
        }

        #endregion

        #region Weight and Capacity Analytics

        public async Task<decimal> GetTotalLoadWeightByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .SumAsync(tl => tl.TotalWeight, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total load weight for date {Date}", date);
                throw;
            }
        }

        public async Task<decimal> GetTruckTotalLoadWeightAsync(int truckId, DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .SumAsync(tl => tl.TotalWeight, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total load weight for truck {TruckId} on date {Date}", truckId, date);
                throw;
            }
        }

        public async Task<Dictionary<int, decimal>> GetDailyLoadWeightsByTruckAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                var loadWeights = await _dbSet
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .GroupBy(tl => tl.TruckId)
                    .Select(g => new { TruckId = g.Key, TotalWeight = g.Sum(tl => tl.TotalWeight) })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return loadWeights.ToDictionary(lw => lw.TruckId, lw => lw.TotalWeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily load weights by truck for date {Date}", date);
                throw;
            }
        }

        public async Task<(decimal MinWeight, decimal MaxWeight, decimal AverageWeight)> GetLoadWeightStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var fromDate = startDate.Date;
                var toDate = endDate.Date.AddDays(1);

                var statistics = await _dbSet
                    .Where(tl => tl.LoadDate >= fromDate && tl.LoadDate < toDate)
                    .GroupBy(tl => 1)
                    .Select(g => new
                    {
                        MinWeight = g.Min(tl => tl.TotalWeight),
                        MaxWeight = g.Max(tl => tl.TotalWeight),
                        AverageWeight = g.Average(tl => tl.TotalWeight)
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                return (statistics?.MinWeight ?? 0, statistics?.MaxWeight ?? 0, statistics?.AverageWeight ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating load weight statistics from {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        #endregion

        #region Cage Management and Optimization

        public async Task<int> GetTotalCagesCountByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .SumAsync(tl => tl.CagesCount, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total cages count for date {Date}", date);
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetCagesCountByTruckAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                var cagesCounts = await _dbSet
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .GroupBy(tl => tl.TruckId)
                    .Select(g => new { TruckId = g.Key, TotalCages = g.Sum(tl => tl.CagesCount) })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return cagesCounts.ToDictionary(cc => cc.TruckId, cc => cc.TotalCages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating cages count by truck for date {Date}", date);
                throw;
            }
        }

        public async Task<decimal> CalculateAverageWeightPerCageAsync(int truckId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var fromDate = startDate.Date;
                var toDate = endDate.Date.AddDays(1);

                var loads = await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= fromDate && tl.LoadDate < toDate && tl.CagesCount > 0)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!loads.Any())
                    return 0;

                var totalWeight = loads.Sum(tl => tl.TotalWeight);
                var totalCages = loads.Sum(tl => tl.CagesCount);

                return totalCages > 0 ? totalWeight / totalCages : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average weight per cage for truck {TruckId} from {StartDate} to {EndDate}",
                    truckId, startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetLoadsByCagesCountRangeAsync(int minCages, int maxCages, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.CagesCount >= minCages && tl.CagesCount <= maxCages)
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loads by cages count range {MinCages}-{MaxCages}", minCages, maxCages);
                throw;
            }
        }

        #endregion

        #region Comprehensive Stub Implementations for Advanced Features

        public async Task<Dictionary<int, decimal>> GetTruckEfficiencyMetricsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var loads = await _dbSet
                    .Where(tl => tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .GroupBy(tl => tl.TruckId)
                    .Select(g => new { TruckId = g.Key, AverageWeight = g.Average(tl => tl.TotalWeight) })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return loads.ToDictionary(l => l.TruckId, l => l.AverageWeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating truck efficiency metrics");
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetHighVolumeLoadsAsync(decimal weightThreshold, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TotalWeight >= weightThreshold)
                    .OrderByDescending(tl => tl.TotalWeight)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving high volume loads");
                throw;
            }
        }

        public async Task<Dictionary<DateTime, decimal>> GetLoadTrendAnalysisAsync(int truckId, int dayRange, CancellationToken cancellationToken = default)
        {
            try
            {
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-dayRange);

                var loads = await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .GroupBy(tl => tl.LoadDate.Date)
                    .Select(g => new { Date = g.Key, TotalWeight = g.Sum(tl => tl.TotalWeight) })
                    .OrderBy(g => g.Date)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return loads.ToDictionary(l => l.Date, l => l.TotalWeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing load trends for truck {TruckId}", truckId);
                throw;
            }
        }

        public async Task<decimal> GetTruckCapacityUtilizationAsync(int truckId, decimal maxCapacity, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var averageLoad = await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .AverageAsync(tl => tl.TotalWeight, cancellationToken)
                    .ConfigureAwait(false);

                return maxCapacity > 0 ? (averageLoad / maxCapacity) * 100 : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating capacity utilization for truck {TruckId}", truckId);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetLoadsForReconciliationAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate && tl.Status == "LOADED")
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loads for reconciliation on {Date}", date);
                throw;
            }
        }

        public async Task<Dictionary<int, (decimal LoadWeight, decimal SoldWeight, decimal Variance)>> GetLoadVsSalesComparisonAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                var loadData = await _dbSet
                    .Where(tl => tl.LoadDate >= targetDate && tl.LoadDate < nextDate)
                    .GroupBy(tl => tl.TruckId)
                    .Select(g => new { TruckId = g.Key, LoadWeight = g.Sum(tl => tl.TotalWeight) })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                // For now, return load data with zero sold weight (would need invoice data integration)
                return loadData.ToDictionary(
                    l => l.TruckId,
                    l => (l.LoadWeight, 0m, l.LoadWeight) // Variance = LoadWeight - SoldWeight
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing load vs sales for {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetUnreconciledLoadsAsync(int dayThreshold, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.Today.AddDays(-dayThreshold);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.LoadDate <= cutoffDate && tl.Status != "RECONCILED")
                    .OrderBy(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unreconciled loads older than {DayThreshold} days", dayThreshold);
                throw;
            }
        }

        public async Task<bool> MarkLoadAsReconciledAsync(int loadId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await UpdateLoadStatusAsync(loadId, "RECONCILED", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking load {LoadId} as reconciled", loadId);
                throw;
            }
        }

        // Additional methods with basic implementations...
        public async Task<IEnumerable<TruckLoad>> SearchLoadsByNotesAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.Notes != null && tl.Notes.Contains(searchTerm))
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching loads by notes: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetLoadsByWeightRangeAsync(decimal minWeight, decimal maxWeight, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TotalWeight >= minWeight && tl.TotalWeight <= maxWeight)
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loads by weight range {MinWeight}-{MaxWeight}", minWeight, maxWeight);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetRecentLoadsAsync(int hours = 24, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-hours);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.LoadDate >= cutoffTime)
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent loads from last {Hours} hours", hours);
                throw;
            }
        }

        public async Task<decimal> GetAverageLoadTimeAsync(int truckId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // Placeholder implementation - would need load time tracking
                var loadCount = await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .CountAsync(cancellationToken)
                    .ConfigureAwait(false);

                return loadCount > 0 ? 45m : 0m; // Default 45 minutes average
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average load time for truck {TruckId}", truckId);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetOptimalLoadsByTruckCapacityAsync(decimal targetCapacity, decimal tolerance, CancellationToken cancellationToken = default)
        {
            try
            {
                var minWeight = targetCapacity - tolerance;
                var maxWeight = targetCapacity + tolerance;

                return await GetLoadsByWeightRangeAsync(minWeight, maxWeight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving optimal loads for capacity {TargetCapacity}", targetCapacity);
                throw;
            }
        }

        public async Task<Dictionary<int, int>> GetTruckLoadFrequencyAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var frequencies = await _dbSet
                    .Where(tl => tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .GroupBy(tl => tl.TruckId)
                    .Select(g => new { TruckId = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return frequencies.ToDictionary(f => f.TruckId, f => f.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating truck load frequency");
                throw;
            }
        }

        public async Task<bool> ValidateLoadDataIntegrityAsync(int loadId, CancellationToken cancellationToken = default)
        {
            try
            {
                var load = await _dbSet.FindAsync(new object[] { loadId }, cancellationToken);

                if (load == null) return false;

                // Basic validation rules
                return load.TotalWeight > 0 &&
                       load.CagesCount > 0 &&
                       !string.IsNullOrEmpty(load.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating load data integrity for {LoadId}", loadId);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetLoadsWithAnomaliesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Define anomaly criteria (e.g., extremely high or low weights)
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.TotalWeight < 100 || tl.TotalWeight > 10000) // Example thresholds
                    .OrderByDescending(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loads with anomalies");
                throw;
            }
        }

        public async Task<bool> RecalculateLoadMetricsAsync(int loadId, CancellationToken cancellationToken = default)
        {
            try
            {
                var load = await _dbSet.FindAsync(new object[] { loadId }, cancellationToken);

                if (load == null) return false;

                // Recalculate any derived metrics here
                load.UpdatedDate = DateTime.Now;

                return await _context.SaveChangesAsync(cancellationToken) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating metrics for load {LoadId}", loadId);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> CreateLoadBatchAsync(IEnumerable<TruckLoad> loads, CancellationToken cancellationToken = default)
        {
            try
            {
                var loadList = loads.ToList();
                await _dbSet.AddRangeAsync(loadList, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created batch of {Count} truck loads", loadList.Count);
                return loadList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating load batch");
                throw;
            }
        }

        public async Task<int> UpdateLoadWeightsBatchAsync(Dictionary<int, decimal> loadWeightUpdates, CancellationToken cancellationToken = default)
        {
            try
            {
                var loadIds = loadWeightUpdates.Keys.ToList();
                var loads = await _dbSet
                    .Where(tl => loadIds.Contains(tl.LoadId))
                    .ToListAsync(cancellationToken);

                foreach (var load in loads)
                {
                    if (loadWeightUpdates.TryGetValue(load.LoadId, out var newWeight))
                    {
                        load.TotalWeight = newWeight;
                        load.UpdatedDate = DateTime.Now;
                    }
                }

                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating load weights");
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetTruckLoadsBatchAsync(IEnumerable<int> truckIds, DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var truckIdsList = truckIds.ToList();
                var targetDate = date.Date;
                var nextDate = targetDate.AddDays(1);

                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => truckIdsList.Contains(tl.TruckId) &&
                                tl.LoadDate >= targetDate &&
                                tl.LoadDate < nextDate)
                    .OrderBy(tl => tl.TruckId)
                    .ThenBy(tl => tl.LoadDate)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch truck loads for date {Date}", date);
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetMonthlyLoadSummaryAsync(int year, int month, CancellationToken cancellationToken = default)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1);

                var summary = await _dbSet
                    .Where(tl => tl.LoadDate >= startDate && tl.LoadDate < endDate)
                    .GroupBy(tl => 1)
                    .Select(g => new
                    {
                        TotalWeight = g.Sum(tl => tl.TotalWeight),
                        TotalLoads = g.Count(),
                        AverageWeight = g.Average(tl => tl.TotalWeight),
                        TotalCages = g.Sum(tl => tl.CagesCount)
                    })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new Dictionary<string, decimal>
                {
                    ["TotalWeight"] = summary?.TotalWeight ?? 0,
                    ["TotalLoads"] = summary?.TotalLoads ?? 0,
                    ["AverageWeight"] = summary?.AverageWeight ?? 0,
                    ["TotalCages"] = summary?.TotalCages ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly summary for {Year}-{Month}", year, month);
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetTopPerformingLoadsAsync(int topCount, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbSet
                    .Include(tl => tl.Truck)
                    .Where(tl => tl.LoadDate >= startDate && tl.LoadDate <= endDate)
                    .OrderByDescending(tl => tl.TotalWeight)
                    .Take(topCount)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top {TopCount} performing loads", topCount);
                throw;
            }
        }

        public async Task<Dictionary<int, decimal>> GetSeasonalLoadPatternsAsync(int truckId, int yearRange, CancellationToken cancellationToken = default)
        {
            try
            {
                var startDate = DateTime.Today.AddYears(-yearRange);
                var patterns = await _dbSet
                    .Where(tl => tl.TruckId == truckId && tl.LoadDate >= startDate)
                    .GroupBy(tl => tl.LoadDate.Month)
                    .Select(g => new { Month = g.Key, AverageWeight = g.Average(tl => tl.TotalWeight) })
                    .OrderBy(g => g.Month)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return patterns.ToDictionary(p => p.Month, p => p.AverageWeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing seasonal patterns for truck {TruckId}", truckId);
                throw;
            }
        }

        #endregion
    }
}