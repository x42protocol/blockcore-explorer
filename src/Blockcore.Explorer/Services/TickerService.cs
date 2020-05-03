using System;
using Blockcore.Explorer.Models;
using Blockcore.Explorer.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NBitcoin;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Blockcore.Explorer.Services
{
   public class TickerService : ServiceBase
   {
      private readonly IMemoryCache memoryCache;
      private readonly ExplorerSettings settings;

      public TickerService() : base(string.Empty)
      {

      }

      public TickerService(IMemoryCache memoryCache, IOptions<ExplorerSettings> settings) : base(settings.Value.Indexer?.ApiUrl)
      {
         this.memoryCache = memoryCache;
         this.settings = settings.Value;
      }

      public string DownloadTicker()
      {
         if (settings.Currency == null || string.IsNullOrWhiteSpace(settings.Ticker.ApiUrl))
         {
            throw new ApplicationException("The Ticker.ApiUrl configuration setting is missing. Unable to download ticker.");
         }

         var client = new RestClient(settings.Ticker.ApiUrl);
         var request = new RestRequest(Method.GET);
         IRestResponse result = client.Execute(request);
         return result.Content;
      }

      public string DownloadRates()
      {
         if (settings.Currency == null || string.IsNullOrWhiteSpace(settings.Currency.ApiUrl))
         {
            throw new ApplicationException("The Currency.ApiUrl configuration setting is missing. Unable to download rates.");
         }

         var client = new RestClient(settings.Currency.ApiUrl);
         var request = new RestRequest(Method.GET);
         IRestResponse result = client.Execute(request);
         return result.Content;
      }

      public Ticker GetTicker(string currency)
      {
         if (!settings.Currency.AutoConvert)
         {
            currency = "USD";
         }

         var ticker = new Ticker();

         string cachedTickerResult = memoryCache.Get<string>("Ticker"); // Responsibility of caching is put on UpdateInfosService.

         if (!string.IsNullOrWhiteSpace(cachedTickerResult))
         {
            var json = JObject.Parse(cachedTickerResult);

            JToken token = json.SelectToken(settings.Ticker.PercentagePath);

            if (token != null)
            {
               if (settings.Ticker.IsBitcoinPrice)
               {
                  Money.TryParse(json.SelectToken(settings.Ticker.PricePath).ToString(), out Money price);
                  ticker.PriceBtc = price;
               }
               else
               {
                  decimal price = (decimal)json.SelectToken(settings.Ticker.PricePath);
                  ticker.Price = price; // Set the USD price, might be replaced with local currency price.
               }

               try
               {
                  // With very little trading, the change might be null and crash.
                  double last24Change = (double)token / 100;
                  ticker.Last24Change = last24Change;
               }
               catch
               {

               }
            }
         }

         string cachedRateResult = memoryCache.Get<string>("Rates"); // Responsibility of caching is put on UpdateInfosService.

         if (!string.IsNullOrWhiteSpace(cachedRateResult))
         {
            var json = JObject.Parse(cachedRateResult);
            JToken rateCurrency = json.SelectToken("$.data[?(@.symbol == '" + currency + "')]");
            JToken rateBtc = json.SelectToken("$.data[?(@.symbol == 'BTC')]");

            ticker.Symbol = (string)rateCurrency.SelectToken("currencySymbol");
            decimal currencyUsdRate = (decimal)rateCurrency.SelectToken("rateUsd");
            decimal btcUsdRate = (decimal)rateBtc.SelectToken("rateUsd");

            if (settings.Ticker.IsBitcoinPrice)
            {
               if (ticker.PriceBtc != null)
               {
                  // First calculate the price of the BTC in USD.
                  decimal usdPrice = ticker.PriceBtc.ToDecimal(MoneyUnit.BTC) * btcUsdRate;

                  // Calculate the price of the USD in the local currency, if different than USD.
                  ticker.Price = (1 / currencyUsdRate) * usdPrice;
               }
            }
            else
            {
               // Take the bitcoin price and multiply with USD price.
               decimal btcPrice = (1 / btcUsdRate) * ticker.Price;
               ticker.PriceBtc = Money.FromUnit(btcPrice, MoneyUnit.BTC);

               // Get the local currency price.
               ticker.Price = (1 / currencyUsdRate) * ticker.Price;
            }
         }

         return ticker;
      }
   }
}
