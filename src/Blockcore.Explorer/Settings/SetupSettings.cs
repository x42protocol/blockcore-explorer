namespace Blockcore.Explorer.Settings
{
   public class SetupSettings
   {
      public SetupSettings()
      {
         Title = "Blockcore Explorer";
      }

      public string Title { get; set; }

      public string Footer { get; set; }

      public string DocumentationUrl { get; set; }
   }
}
