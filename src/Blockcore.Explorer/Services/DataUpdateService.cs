using System;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Explorer.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Explorer.Services
{
   public class DataUpdateService : IHostedService, IDisposable
   {
      private readonly ILogger<DataUpdateService> log;
      private readonly BlockIndexService blockIndexService;
      private readonly TickerService tickerService;
      private readonly WeightService weightService;
      private readonly IMemoryCache memoryCache;
      private readonly ExplorerSettings settings;
      private System.Timers.Timer indexerTimer;
      private System.Timers.Timer tickerTimer;
      private System.Timers.Timer weightTimer;

      public DataUpdateService(
         ILogger<DataUpdateService> log,
          BlockIndexService blockIndexService,
          TickerService tickerService,
          WeightService weightService,
          IMemoryCache memoryCache,
          IOptions<ExplorerSettings> settings, IOptions<ChainSettings> chainSettings)
      {
         this.log = log;
         this.tickerService = tickerService;
         this.weightService = weightService;
         this.blockIndexService = blockIndexService;
         this.memoryCache = memoryCache;
         this.settings = settings.Value;

         this.log.LogInformation($"CHAIN SYMBOL: {chainSettings.Value.Symbol}");
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         if (settings.Features == null)
         {
            return Task.CompletedTask;
         }

         if (settings.Features.Explorer)
         {
            try
            {
               indexerTimer = new System.Timers.Timer();
               StartIndexerTimer();
            }
            catch (Exception ex)
            {
               log.LogCritical(ex, "Failed to start indexer timer.");
            }
         }

         if (settings.Features.Ticker)
         {
            try
            {
               tickerTimer = new System.Timers.Timer();
               StartTickerTimer(cancellationToken);
            }
            catch (Exception ex)
            {
               log.LogCritical(ex, "Failed to start ticker timer.");
            }
         }

         if (settings.Features.POSWeight)
         {
            try
            {
               weightTimer = new System.Timers.Timer();
               StartWeightTimer();
            }
            catch (Exception ex)
            {
               log.LogCritical(ex, "Failed to start POS weight timer.");
            }
         }

         return Task.CompletedTask;
      }

      private void StartIndexerTimer()
      {
         indexerTimer.AutoReset = false; // Make sure it only trigger once initially.

         indexerTimer.Elapsed += (sender, args) =>
         {
            if (indexerTimer.AutoReset == false)
            {
               indexerTimer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
               indexerTimer.AutoReset = true;
            }

            try
            {
               // Get statistics and cache it.
               memoryCache.Set("BlockchainStats", blockIndexService.GetStatistics());
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Failed to set blockchain stats cache.");
            }
         };

         indexerTimer.Start();
      }

      private void StartTickerTimer(CancellationToken cancellationToken)
      {
         tickerTimer.AutoReset = false; // Make sure it only trigger once initially.

         tickerTimer.Elapsed += async (sender, args) =>
         {
            if (tickerTimer.AutoReset == false)
            {
               tickerTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
               tickerTimer.AutoReset = true;
            }

            try
            {
               // Get ticker and cache it.
               memoryCache.Set("Ticker", tickerService.DownloadTicker());
               memoryCache.Set("Rates", tickerService.DownloadRates());
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Failed to set ticker or rates.");
            }
         };

         tickerTimer.Start();
      }

      private void StartWeightTimer()
      {
         weightTimer.AutoReset = false; // Make sure it only trigger once initially.

         weightTimer.Elapsed += (sender, args) =>
         {
            if (weightTimer.AutoReset == false)
            {
               weightTimer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
               weightTimer.AutoReset = true;
            }

            try
            {
               // Get staking info and cache it.
               memoryCache.Set("StakingInfo", weightService.DownloadStakingInfo());
            }
            catch (Exception ex)
            {
               log.LogError(ex, "Failed to set staking info.");
            }
         };

         weightTimer.Start();
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         return Task.CompletedTask;
      }

      public void Dispose()
      {
         indexerTimer?.Stop();
         indexerTimer?.Dispose();

         tickerTimer?.Stop();
         tickerTimer?.Dispose();

         weightTimer?.Stop();
         weightTimer?.Dispose();
      }
   }
}
