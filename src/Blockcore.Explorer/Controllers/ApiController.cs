using System;
using System.Globalization;
using Blockcore.Explorer.Models;
using Blockcore.Explorer.Services;
using Blockcore.Explorer.Settings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace Blockcore.Explorer.Controllers
{
   [Route("api")]
   [ApiController]
   public class ApiController : ControllerBase
   {
      private readonly ExplorerSettings settings;
      private readonly IMemoryCache memoryCache;
      private readonly TickerService tickerService;
      private readonly CurrencyService currencyService;
      private readonly ILogger<ApiController> log;

      public ApiController(
          ILogger<ApiController> log,
          TickerService tickerService,
          CurrencyService currencyService,
          IMemoryCache memoryCache,
          IOptions<ExplorerSettings> settings)
      {
         this.log = log;
         this.tickerService = tickerService;
         this.currencyService = currencyService;
         this.memoryCache = memoryCache;
         this.settings = settings.Value;
      }

      [HttpGet]
      [Route("price")]
      public ActionResult<object> Price(bool notApi = false, decimal amount = 1)
      {
         try
         {
            IRequestCultureFeature rqf = Request.HttpContext.Features.Get<IRequestCultureFeature>();
            RegionInfo regionInfo = currencyService.GetRegionaInfo(rqf);
            Ticker ticker = tickerService.GetTicker(regionInfo.ISOCurrencySymbol);

            if (notApi)
            {
               return new TickerApi
               {
                  Symbol = ticker.Symbol,
                  PriceBtc = ticker.PriceBtc.ToString(),
                  Price = ticker.Price.ToString("C2"),
                  Last24Change = (ticker.Last24Change).ToString("P2")
               };
            }

            return ticker;
         }
         catch (Exception ex)
         {
            log.LogError(ex, "Failed to get price from API.");
            return null;
         }
      }

      //[HttpGet]
      //[Route("address/{address}")]
      //public ActionResult<object> Address(string address)
      //{
      //   try
      //   {
      //      var endpointClient = new RestClient($"{settings.Indexer.ApiUrl}query/address/{address}/transactions");
      //      var endpointRequest = new RestRequest(Method.GET);
      //      endpointRequest.AddQueryParameter("api-version", "1.0");
      //      log.LogInformation($"Querying the indexer with URL: " + endpointRequest.ToString());
      //      IRestResponse endpointResponse = endpointClient.Execute(endpointRequest);
      //      return JsonConvert.DeserializeObject(endpointResponse.Content);

      //   }
      //   catch (Exception ex)
      //   {
      //      log.LogError(ex, "Failed to get address.");
      //   }

      //   return null;
      //}

      //[HttpGet]
      //[Route("transaction/{transaction}")]
      //public ActionResult<object> Transaction(string transaction)
      //{
      //   try
      //   {
      //      var endpointClient = new RestClient($"{settings.Indexer.ApiUrl}query/transaction/{transaction}");
      //      var endpointRequest = new RestRequest(Method.GET);
      //      endpointRequest.AddQueryParameter("api-version", "1.0");
      //      log.LogInformation($"Querying the indexer with URL: " + endpointRequest.ToString());
      //      IRestResponse endpointResponse = endpointClient.Execute(endpointRequest);
      //      return JsonConvert.DeserializeObject(endpointResponse.Content);
      //   }
      //   catch (Exception ex)
      //   {
      //      log.LogError(ex, "Failed to get transaction.");
      //   }

      //   return null;
      //}

      //[HttpGet]
      //[Route("block/{block}")]
      //public ActionResult<object> Block(string block)
      //{
      //   try
      //   {
      //      var endpointClient = new RestClient($"{settings.Indexer.ApiUrl}query/block/index/{block}/transactions");
      //      var endpointRequest = new RestRequest(Method.GET);
      //      endpointRequest.AddQueryParameter("api-version", "1.0");
      //      log.LogInformation($"Querying the indexer with URL: " + endpointRequest.ToString());
      //      IRestResponse endpointResponse = endpointClient.Execute(endpointRequest);
      //      return JsonConvert.DeserializeObject(endpointResponse.Content);
      //   }
      //   catch (Exception ex)
      //   {
      //      log.LogError(ex, "Failed to get block.");
      //   }

      //   return null;
      //}
   }
}
