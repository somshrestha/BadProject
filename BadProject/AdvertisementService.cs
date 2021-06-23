using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using ThirdParty;

namespace Adv
{
    public class AdvertisementService
    {
        private static MemoryCache cache = new MemoryCache("");
        private static Queue<DateTime> errors = new Queue<DateTime>();

        private Object lockObj = new Object();
        // **************************************************************************************************
        // Loads Advertisement information by id
        // from cache or if not possible uses the "mainProvider" or if not possible uses the "backupProvider"
        // **************************************************************************************************
        // Detailed Logic:
        // 
        // 1. Tries to use cache (and retuns the data or goes to STEP2)
        //
        // 2. If the cache is empty it uses the NoSqlDataProvider (mainProvider), 
        //    in case of an error it retries it as many times as needed based on AppSettings
        //    (returns the data if possible or goes to STEP3)
        //
        // 3. If it can't retrive the data or the ErrorCount in the last hour is more than 10, 
        //    it uses the SqlDataProvider (backupProvider)
        public Advertisement GetAdvertisement(string id)
        {
            Advertisement adv;

            lock (lockObj)
            {
                // Use Cache if available
                adv = (Advertisement)cache.Get($"AdvKey_{id}");

                var errorCount = CountHttpErrorsInLastHour();

                switch (adv)
                {
                    // If Cache is empty and ErrorCount<10 then use HTTP provider
                    // if needed try to use Backup provider
                    case null when (errorCount < 10):
                        adv = UseHttpProvider(id);
                        break;
                    case null:
                        adv = UseBackupProvider(id);
                        break;
                }
            }

            return adv;
        }

        private static Advertisement UseBackupProvider(string id)
        {
            var adv = SQLAdvProvider.GetAdv(id);

            if (adv != null) 
                SetCache(id, adv);

            return adv;
        }

        private static Advertisement UseHttpProvider(string id)
        {
            Advertisement adv = null;
            var retry = 0;

            do
            {
                retry++;
                try
                {
                    var dataProvider = new NoSqlAdvProvider();
                    adv = dataProvider.GetAdv(id);
                }
                catch
                {
                    Thread.Sleep(1000);
                    errors.Enqueue(DateTime.Now); // Store HTTP error timestamp              
                }
            } while ((adv == null) && (retry < int.Parse(ConfigurationManager.AppSettings["RetryCount"])));

            if (adv != null) 
                SetCache(id, adv);

            return adv;
        }

        private static int CountHttpErrorsInLastHour()
        {
            // Count HTTP error timestamps in the last hour
            while (errors.Count > 20) errors.Dequeue();

            return errors.Count(dat => dat > DateTime.Now.AddHours(-1));
        }

        private static void SetCache(string id, Advertisement adv)
        {
            cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(5));
        }
    }
}
