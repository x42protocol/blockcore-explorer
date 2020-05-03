using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Explorer.Settings;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Blockcore.Explorer.TagHelpers
{
   public class TimeTagHelper : TagHelper
   {
      private readonly ExplorerSettings settings;

      private readonly ChainSettings chainSettings;

      private readonly ILogger<TimeTagHelper> log;

      [HtmlAttributeName("Hide")]
      public bool? Hide { get; set; }

      [HtmlAttributeName("Since")]
      public DateTime? Since { get; set; }

      public TimeTagHelper(IOptions<ExplorerSettings> settings, IOptions<ChainSettings> chainSettings, ILogger<TimeTagHelper> log)
      {
         this.settings = settings.Value;
         this.chainSettings = chainSettings.Value;
         this.log = log;
      }

      private string FormatTimeSpan(TimeSpan timeSpan)
      {
         Func<Tuple<int, string>, string> tupleFormatter = t => $"{t.Item1} {t.Item2}{(t.Item1 == 1 ? string.Empty : "s")}";
         var components = new List<Tuple<int, string>>
        {
            Tuple.Create((int) timeSpan.TotalDays, "day"),
            Tuple.Create(timeSpan.Hours, "hour"),
            Tuple.Create(timeSpan.Minutes, "minute"),
            Tuple.Create(timeSpan.Seconds, "second"),
        };

         components.RemoveAll(i => i.Item1 == 0);

         string extra = "";

         if (components.Count > 1)
         {
            Tuple<int, string> finalComponent = components[components.Count - 1];
            components.RemoveAt(components.Count - 1);
            extra = $" and {tupleFormatter(finalComponent)}";
         }

         return $"{string.Join(", ", components.Select(tupleFormatter))}{extra}";
      }

      public override void Process(TagHelperContext context, TagHelperOutput output)
      {
         if (Hide.HasValue && Hide.Value)
         {
            output.Content.SetContent("-");
         }
         else
         {
            string input = output.GetChildContentAsync().Result.GetContent();
            bool success = long.TryParse(input, out long value);

            if (success)
            {
               try
               {
                  string text = string.Empty;

                  // If the "since" attribute has been specified, we'll calculate an offset.
                  if (Since.HasValue)
                  {
                     TimeSpan time = (Since.Value - DateTimeOffset.FromUnixTimeSeconds(value));
                     text = GetReadableTimespan(time);
                  }
                  else // Normally just show the time in a human readable form.
                  {
                     text = DateTimeOffset.FromUnixTimeSeconds(value).ToString("F");
                  }

                  string html = $"<span class=\"time-value\">{text}</span>";
                  output.Content.SetHtmlContent(html);
               }
               catch (Exception ex)
               {
                  log.LogError(ex, $"Failed to parse in TimeTagHelper. Input was {input} and parsed value was {value}.");
                  output.Content.SetContent(input);
               }
            }
            else
            {
               output.Content.SetContent(input);
            }
         }
      }

      public string GetReadableTimespan(TimeSpan ts)
      {
         // formats and its cutoffs based on totalseconds
         var cutoff = new SortedList<long, string> {
          {59, "{3:S}" },
          {60, "{2:M}" },
          {60*60-1, "{2:M}, {3:S}"},
          {60*60, "{1:H}"},
          {24*60*60-1, "{1:H}, {2:M}"},
          {24*60*60, "{0:D}"},
          {Int64.MaxValue , "{0:D}, {1:H}"}
        };

         // find nearest best match
         int find = cutoff.Keys.ToList().BinarySearch((long)ts.TotalSeconds);
         // negative values indicate a nearest match
         int near = find < 0 ? Math.Abs(find) - 1 : find;
         // use custom formatter to get the string
         return String.Format(
             new HMSFormatter(),
             cutoff[cutoff.Keys[near]],
             ts.Days,
             ts.Hours,
             ts.Minutes,
             ts.Seconds);
      }

   }

   // formatter for forms of
   // seconds/hours/day
   // Source: https://stackoverflow.com/a/21649465
   public class HMSFormatter : ICustomFormatter, IFormatProvider
   {
      // list of Formats, with a P customformat for pluralization
      static readonly Dictionary<string, string> timeformats = new Dictionary<string, string> {
        {"S", "{0:P:Seconds:Second}"},
        {"M", "{0:P:Minutes:Minute}"},
        {"H","{0:P:Hours:Hour}"},
        {"D", "{0:P:Days:Day}"}
    };

      public string Format(string format, object arg, IFormatProvider formatProvider)
      {
         return String.Format(new PluralFormatter(), timeformats[format], arg);
      }

      public object GetFormat(Type formatType)
      {
         return formatType == typeof(ICustomFormatter) ? this : null;
      }
   }

   // formats a numeric value based on a format P:Plural:Singular
   public class PluralFormatter : ICustomFormatter, IFormatProvider
   {

      public string Format(string format, object arg, IFormatProvider formatProvider)
      {
         if (arg != null)
         {
            string[] parts = format.Split(':'); // ["P", "Plural", "Singular"]

            if (parts[0] == "P") // correct format?
            {
               // which index postion to use
               int partIndex = (arg.ToString() == "1") ? 2 : 1;
               // pick string (safe guard for array bounds) and format
               return String.Format("{0} {1}", arg, (parts.Length > partIndex ? parts[partIndex] : ""));
            }
         }
         return String.Format(format, arg);
      }

      public object GetFormat(Type formatType)
      {
         return formatType == typeof(ICustomFormatter) ? this : null;
      }
   }
}
