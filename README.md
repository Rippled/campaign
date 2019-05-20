# Campaign
Campaign monitor wrapper

# example of use

    public class HomeController : Controller
        {
            private readonly string _listId;
            private readonly string _apiKey;
            private readonly CampaignRepository _repo;
            public HomeController()
            {
                _listId = "someid";
                _apiKey = somekeyT19//qAeTA14IQ==";
                _repo = new CampaignRepository(_apiKey, _listId, 400);
            }
            public ActionResult Index()
            {
                var viewModel = new HomeViewModel()
                {
                    Users = _repo.GetSubscribers(),
                    Unsubscribed = _repo.GetUnsubscribed()
                };
                return View(viewModel);
            }       
        }
