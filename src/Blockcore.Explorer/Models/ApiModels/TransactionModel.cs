namespace Blockcore.Explorer.Models.ApiModels
{
   public class TransactionModel
   {
      public int Index { get; set; }

      public string Type { get; set; }

      public string TransactionHash { get; set; }

      public string SpendingTransactionHash { get; set; }

      public string PubScriptHex { get; set; }

      public bool CoinBase { get; set; }

      public bool CoinStake { get; set; }

      public long Value { get; set; }

      public long BlockIndex { get; set; }

      public long Confirmations { get; set; }

      public int Time { get; set; }
   }
}
