using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Blockcore.Explorer.Models.ApiModels
{
   public class TransactionDetailsModel
   {
      public TransactionDetailsModel()
      {
         Inputs = new List<TransactionInputModel>();
         Outputs = new List<TransactionOutputModel>();
      }

      public string Symbol { get; set; }

      public string BlockHash { get; set; }

      public long BlockIndex { get; set; }

      [DataType(DataType.Date)]
      public DateTime Timestamp { get; set; }

      public string TransactionId { get; set; }

      public int Confirmations { get; set; }

      public bool IsCoinbase { get; set; }

      public bool IsCoinstake { get; set; }

      [JsonProperty(PropertyName = "rbf")]
      public bool ReplaceByFee { get; set; }

      public int Version { get; set; }

      public List<TransactionInputModel> Inputs { get; set; }

      public List<TransactionOutputModel> Outputs { get; set; }
   }
}
