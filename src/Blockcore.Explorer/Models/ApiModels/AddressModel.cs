using System.Collections.Generic;

namespace Blockcore.Explorer.Models.ApiModels
{
   public class AddressModel
   {
      public AddressModel()
      {
         Transactions = new List<TransactionModel>();
         UnconfirmedTransactions = new List<TransactionModel>();
      }

      public string Symbol { get; set; }

      public string Address { get; set; }

      public long Balance { get; set; }

      public long TotalReceived { get; set; }

      public long TotalSent { get; set; }

      public long UnconfirmedBalance { get; set; }

      public List<TransactionModel> Transactions { get; set; }

      public List<TransactionModel> UnconfirmedTransactions { get; set; }
   }
}
