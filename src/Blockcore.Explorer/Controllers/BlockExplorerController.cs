using System;
using System.Collections.Generic;
using Blockcore.Explorer.Models;
using Blockcore.Explorer.Models.ApiModels;
using Blockcore.Explorer.Services;
using Blockcore.Explorer.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace Blockcore.Explorer.Controllers
{
   [ApiExplorerSettings(IgnoreApi = true)]
   [Route("block-explorer")]
   public class BlockExplorerController : Controller
   {
      private readonly ExplorerSettings settings;
      private readonly ChainSettings chainSettings;
      private readonly IMemoryCache memoryCache;
      private readonly Status stats;
      private readonly BlockIndexService indexService;

      public BlockExplorerController(IMemoryCache memoryCache,
          BlockIndexService indexService,
          IOptions<ExplorerSettings> settings,
          IOptions<ChainSettings> chainSettings)
      {
         this.memoryCache = memoryCache;

         if (this.memoryCache.Get("BlockchainStats") != null)
         {
            stats = JsonConvert.DeserializeObject<Status>(this.memoryCache.Get("BlockchainStats").ToString());
         }
         else
         {
            stats = new Status { Error = "BlockchainStats not available yet." };
         }

         this.indexService = indexService;
         this.settings = settings.Value;
         this.chainSettings = chainSettings.Value;
      }

      [HttpGet]
      public IActionResult Index()
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         try
         {
            BlockModel latestBlock = indexService.GetLatestBlock();

            ViewBag.LatestBlock = latestBlock;
            ViewBag.BlockchainHeight = latestBlock.BlockIndex;

            var latestBlocks = new List<dynamic>();
            latestBlocks.Add(latestBlock);

            for (int i = (int)ViewBag.LatestBlock.BlockIndex - 1; i >= (int)ViewBag.LatestBlock.BlockIndex - 5; i--)
            {
               latestBlocks.Add(indexService.GetBlockByHeight(i));
            }

            ViewBag.Blocks = latestBlocks;

            return View();
         }
         catch (Exception ex)
         {
            ViewBag.Error = ex;
            return View();
         }
      }

      [HttpGet]
      [Route("search")]
      public IActionResult Search(SearchBlockExplorer searchBlockExplorer)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         if (searchBlockExplorer.Query == null)
         {
            return RedirectToAction("Index");
         }
         else if (searchBlockExplorer.Query.Length == 34)
         {
            return RedirectToAction("Address", new { address = searchBlockExplorer.Query });
         }
         else if (searchBlockExplorer.Query.Length == 64)
         {
            return RedirectToAction("Transaction", new { transactionId = searchBlockExplorer.Query });
         }
         else if (searchBlockExplorer.Query.Length <= 8)
         {
            return RedirectToAction("Block", new { block = searchBlockExplorer.Query });
         }

         return RedirectToAction("Index");
      }

      [HttpGet]
      [Route("block/{block}")]
      public IActionResult Block(string block)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         BlockModel result = (block.ToLower() == "latest") ? indexService.GetLatestBlock() : indexService.GetBlockByHeight(int.Parse(block));
         return View(result);
      }

      [HttpGet]
      [Route("block/hash/{hash}")]
      public IActionResult BlockHash(string hash)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;
         BlockModel block = indexService.GetBlockByHash(hash);
         return View("Block", block);
      }

      [HttpGet]
      [Route("address/{address}")]
      public IActionResult Address(string address)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         return View(indexService.GetTransactionsByAddress(address));
      }

      [HttpGet]
      [Route("transaction/{transactionId}")]
      public IActionResult Transaction(string transactionId)
      {
         ViewBag.Features = settings.Features;
         ViewBag.Setup = settings.Setup;
         ViewBag.Chain = chainSettings;

         ViewBag.BlockchainHeight = stats.SyncBlockIndex;

         TransactionDetailsModel trx = indexService.GetTransaction(transactionId);

         return View(trx);
      }
   }
}
