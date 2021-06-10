using CamlBuilder;
using log4net;
using log4net.Config;
using Microsoft.SharePoint.Client;
using PnP.Framework.Utilities;
using System;
using System.IO;

namespace csom_connection_error_test
{
   internal class Program
   {
      private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

      private static void Main(string[] args)
      {
         var fileInfo = new FileInfo(Properties.Settings.Default.LogConfigPath);
         XmlConfigurator.Configure(fileInfo);

         try
         {
            Log.Info("Starting...");
            var startDate = DateTime.Now;
            var ctx = GetAppOnlyContext(Properties.Settings.Default.Url);
            while (true)
            {
               try
               {               
                  if ((DateTime.Now - startDate).TotalMinutes > 30)
                  {
                     Log.Info("==> Recreating context");
                     ctx.Dispose();
                     Log.Info("Context disposed");
                     ctx = GetAppOnlyContext(Properties.Settings.Default.Url);                  
                     startDate = DateTime.Now;
                     Log.Info($"Context created at {startDate}");
                  }

                  var listUrl = UrlUtility.Combine(ctx.Url, "/Lists/YOUR_LIST_NAME_GOES_HERE");
                  Log.Info($"The list URL is {listUrl}");
                  var list = ctx.Web.GetList(listUrl);
                  var items = list.GetItems(CamlQuery.CreateAllItemsQuery());               
                  ctx.Load(items);
                  ctx.IdiExecuteQueryRetry();
                  Log.Debug($"Items retrieved");

                  foreach (var item in items)
                  {
                     var reQueriedItem = list.GetItemById(item.Id);
                     ctx.Load(reQueriedItem);
                     ctx.IdiExecuteQueryRetry();
                     Log.Debug($"Item with the Id '{item.Id}' retrieved");
                  }
               }
               catch (Exception ex)
               {
                  Log.Fatal($"Fatal problem encountered", ex);
               }
            }            
         }
         catch (Exception ex)
         {
            Log.Fatal($"Program crashed", ex);
         }

         Console.ReadKey();
         Log.Logger.Repository.Shutdown(); // allow async logging to run to completion
      }

      private static ClientContext GetAppOnlyContext(string siteUrl)
      {
         Uri siteUri = new Uri(siteUrl);
         string realm = IdiTokenHelper.GetRealmFromTargetUrl(siteUri);
         string accessToken = IdiTokenHelper.GetAppOnlyAccessToken(IdiTokenHelper.SharePointPrincipal, siteUri.Authority, realm).AccessToken;
         return IdiTokenHelper.GetClientContextWithAccessToken(siteUri.ToString(), accessToken);
      }
   }
}
