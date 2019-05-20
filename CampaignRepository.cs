using createsend_dotnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public class CampaignRepository
    {
        private readonly AuthenticationDetails _auth;
        private readonly string _listId;
        private readonly DateTime _past;
        private readonly Dictionary<string, int> _verbsDict;
        public CampaignRepository(string apiKey, string listId, int timePeriod)
        {
            _auth = new ApiKeyAuthenticationDetails(apiKey);
            _listId = listId;
            _past = DateTime.Now.AddDays(-timePeriod);
            _verbsDict = new Dictionary<string, int>()
            {
                { "Active", 16},
                { "Unsubscribed", 20},
                { "Deleted", 24},
                { "Bounced", 22}
            };
        }        
        public string AddSubscriber(CampaignUser user, bool resubscribe = false)
        {
            Subscriber subscriber = new Subscriber(_auth, _listId);
            string result = string.Empty;
            try
            {
                string newSubscriberID = subscriber
                    .Add(user.Email, user.Name, null, resubscribe, ConsentToTrack.Unchanged);
                result = newSubscriberID;
            }
            catch (CreatesendException ex)
            {
                ErrorResult error = (ErrorResult)ex.Data["ErrorResult"];
                throw new ApplicationException(error.Code + " " + error.Message);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.ToString());
            }
            return result;
        }
        public void AddSubscribers(List<CampaignUser> users, bool resubscribe = false)
        {
            Subscriber subscriber = new Subscriber(_auth, _listId);
            List<SubscriberDetail> newSubscribers = new List<SubscriberDetail>();
            foreach (var user in users)
            {
                newSubscribers.Add(new SubscriberDetail()
                {
                    Name = user.Name,
                    EmailAddress = user.Email,
                    ConsentToTrack = ConsentToTrack.Unchanged,
                    CustomFields = null
                });
            }
            try
            {
                BulkImportResults results = subscriber.Import(newSubscribers, resubscribe);
                //Console.WriteLine(results.TotalNewSubscribers + " subscribers added");
                //Console.WriteLine(results.TotalExistingSubscribers + " total subscribers in list");
            }
            catch (CreatesendException ex)
            {
                ErrorResult<BulkImportResults> error = (ErrorResult<BulkImportResults>)ex.Data["ErrorResult"];
                //Console.WriteLine(error.Code);
                //Console.WriteLine(error.Message);
                var sd = new StringBuilder(string.Empty);
                if (error.ResultData != null)
                {
                    //handle the returned data
                    BulkImportResults results = error.ResultData;
                    sd.AppendLine("Failed Address");
                    //success details are here as normal
                    //Console.WriteLine(results.TotalNewSubscribers + " subscribers were still added");
                    //but we also have additional failure detail
                    foreach (ImportResult result in results.FailureDetails)
                    {
                        //Console.WriteLine("Failed Address");
                        //Console.WriteLine(result.Message + " - " + result.EmailAddress);
                        sd.AppendLine(result.EmailAddress + " - " + result.Message);
                    }
                }
                throw new ApplicationException(sd.ToString());
            }
            catch (Exception ex)
            {
                // Handle some other failure
                //Console.WriteLine(ex.ToString());
                throw new ApplicationException(ex.ToString());
            }

        }
        public void DeleteSubscriber(CampaignUser user)
        {
            Subscriber subscriber = new Subscriber(_auth, _listId);
            subscriber.Delete(user.Email);
        }
        public List<CampaignUser> GetSubscribers() => Get("Active");
        public List<CampaignUser> GetUnsubscribed() => Get("Unsubscribed");
        public List<CampaignUser> GetDeleted() => Get("Deleted");
        public List<CampaignUser> GetBounced() => Get("Bounced");
        private List<CampaignUser> Get(string verb)
        {
            List list = new List(_auth, _listId);
            
            List<SubscriberDetail> allSubscribers = new List<SubscriberDetail>();
            try
            {
                // get the first page, with an old date to signify entire list
                MethodInfo mi = list.GetType().GetMethods()[_verbsDict[verb]];
                PagedCollection<SubscriberDetail> firstPage = (PagedCollection<SubscriberDetail>)mi.Invoke(list, new object[] { _past, 1, 50, "Email", "ASC", false });
                allSubscribers.AddRange(firstPage.Results);
                if (firstPage.NumberOfPages > 1)
                {
                    for (int pageNumber = 2; pageNumber <= firstPage.NumberOfPages; pageNumber++)
                    {
                        PagedCollection<SubscriberDetail> subsequentPage = (PagedCollection<SubscriberDetail>)mi.Invoke(list, new object[] { _past, pageNumber, 50, "Email", "ASC", false });
                        allSubscribers.AddRange(subsequentPage.Results);
                    }
                }
            }
            catch (CreatesendException ex)
            {
                ErrorResult error = (ErrorResult)ex.Data["ErrorResult"];
                throw new ApplicationException(error.Code + " " + error.Message);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.ToString());
            }
            return allSubscribers.Select(m => new CampaignUser() { Name = m.Name, Email = m.EmailAddress, LastUpdated = m.Date }).ToList();
        }
    }
}
