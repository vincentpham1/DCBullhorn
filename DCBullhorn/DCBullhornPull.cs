using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using DeltaCompanies;

namespace DCBullhorn
{
    public partial class DCBullhornPull : ServiceBase
    {
        public static HttpClient ApiClient { get; set; }

        private static string username = "APIuserDev.TDC";
        private static string password = "deltadev123!";
        private static string clientId = "ea0a6da6-937f-4a53-b931-4f1c9da0f5e8";
        private static string clientSecret = "Av7TXnvYO5g9tAElv9NF4BiUccQiXV9K";

        public static System.Timers.Timer _timer;

        public DCBullhornPull()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Debugger.Launch();

            try
            {
                DeltaCompaniesDBEntities DeltaDb = new DeltaCompaniesDBEntities();

                var interval = DeltaDb.TimedServices.First();
                var milliseconds = (interval.Hours * 3600000) + (interval.Minutes * 60000);
                ExecuteService(milliseconds);
            }

            catch (Exception ex)
            {
                ErrorLogger.LogError(ex);
            }
        }

        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Stop();

                ApiClient = new HttpClient();
                ApiClient.DefaultRequestHeaders.Accept.Clear();
                ApiClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var authContent = new StringContent("");

                using (HttpResponseMessage authResponse = await ApiClient.PostAsync($"https://auth.bullhornstaffing.com/oauth/authorize?client_id={clientId}&response_type=code&username={username}&password={password}&action=Login", authContent))
                {
                    if (authResponse.IsSuccessStatusCode)
                    {
                        string redirectURL = authResponse.RequestMessage.RequestUri.ToString();
                        string authCode = redirectURL.Substring(redirectURL.IndexOf("code=") + 5);
                        authCode = authCode.Remove(authCode.IndexOf("&"));

                        #region Substring token response json to get Access and Refresh Tokens

                        string tokenResponseData = GetTokens(authCode);

                        string accessToken = tokenResponseData.Substring(tokenResponseData.IndexOf("access_token") + 17);
                        accessToken = accessToken.Remove(accessToken.IndexOf("\""));

                        string refreshToken = tokenResponseData.Substring(tokenResponseData.IndexOf("refresh_token") + 18);
                        refreshToken = refreshToken.Remove(refreshToken.IndexOf("\""));

                        #endregion

                        //Url that uses refresh token to get a new access token. Access tokens are valid for 10 minutes
                        //https://auth.bullhornstaffing.com/oauth/token?grant_type=refresh_token&refresh_token={refresh_token}&client_id={client_id}&client_secret={client_secret}

                        #region Substring session response json to get BhRestToken and restUrl

                        string sessionResponseData = GetSessionKey(accessToken);

                        string bhRestToken = sessionResponseData.Substring(sessionResponseData.IndexOf("BhRestToken") + 14);
                        bhRestToken = bhRestToken.Remove(bhRestToken.IndexOf("\""));

                        string restURL = sessionResponseData.Substring(sessionResponseData.IndexOf("restUrl") + 10);
                        restURL = restURL.Remove(restURL.IndexOf("\""));

                        #endregion

                        HttpClient DataClient = new HttpClient();
                        DataClient.BaseAddress = new Uri(restURL);
                        DataClient.DefaultRequestHeaders.Accept.Clear();
                        DataClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        DataClient.DefaultRequestHeaders.Add("BhRestToken", bhRestToken);

                        //Query URL can be changed as needed to get required data. Current URL is for example/testing
                        using (HttpResponseMessage dataResponse = await DataClient.GetAsync($"query/JobOrder?where=isDeleted=false&where=isPublic=1&where=isOpen=true&fields=id,title,status"))
                        {
                            if (dataResponse.IsSuccessStatusCode)
                            {
                                //Parsing JSON will change depending on data pulled and number of records
                                string jobData = await dataResponse.Content.ReadAsStringAsync();

                                string recordCount = jobData.Substring(jobData.IndexOf("count") + 7);
                                recordCount = recordCount.Remove(recordCount.IndexOf("\""));
                                recordCount = recordCount.Replace(",", "");

                                for (int i = 0; i < int.Parse(recordCount); i++)
                                {
                                    string strJobID = jobData.Substring(jobData.IndexOf("id") + 4);
                                    strJobID = strJobID.Remove(strJobID.IndexOf("\""));
                                    strJobID = strJobID.Replace(",", "");
                                    int jobID = int.Parse(strJobID);

                                    string jobTitle = jobData.Substring(jobData.IndexOf("title") + 8);
                                    jobTitle = jobTitle.Remove(jobTitle.IndexOf("\""));
                                    jobTitle = jobTitle.Replace(",", "");

                                    string status = jobData.Substring(jobData.IndexOf("status") + 9);
                                    status = status.Remove(status.IndexOf("\""));
                                    status = status.Replace(",", "");

                                    jobData = jobData.Substring(jobData.IndexOf("},") + 2);

                                    DeltaCompaniesDBEntities DeltaDb = new DeltaCompaniesDBEntities();

                                    if (status != "null" && !DeltaDb.Status.Any(x => x.Name == status))
                                    {
                                        DeltaDb.Status.Add(new Status { Name = status });
                                        DeltaDb.SaveChanges();
                                    }

                                    if (!DeltaDb.JobOrders.Any(x => x.ID == jobID))
                                    {
                                        //StatusID == 10 is N/A (Random default status I made up)
                                        DeltaDb.JobOrders.Add(new JobOrder { ID = jobID, Title = jobTitle, StatusID = (status == "null" ? 10 : DeltaDb.Status.First(x => x.Name == status).ID) });
                                        DeltaDb.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }

                _timer.Start();
            }

            catch (Exception ex)
            {
                ErrorLogger.LogError(ex);
            }
        }

        protected override void OnStop()
        {
            
        }

        private string GetTokens(string authCode)
        {
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create($"https://auth.bullhornstaffing.com/oauth/token?grant_type=authorization_code&code={authCode}&client_id={clientId}&client_secret={clientSecret}");
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/json";

            HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();

            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                using (System.IO.Stream tokenStream = tokenResponse.GetResponseStream())
                {
                    if (tokenStream != null)
                    {
                        using (System.IO.StreamReader tokenReader = new System.IO.StreamReader(tokenStream))
                        {
                            return tokenReader.ReadToEnd();
                        }
                    }

                    else
                    {
                        return "";
                    }
                }
            }

            else
            {
                return "";
            }
        }

        private string GetSessionKey(string accessToken)
        {
            HttpWebRequest sessionRequest = (HttpWebRequest)WebRequest.Create($"https://rest.bullhornstaffing.com/rest-services/login?version=*&access_token={accessToken}");
            sessionRequest.Method = "GET";
            sessionRequest.ContentType = "application/json";

            HttpWebResponse sessionResponse = (HttpWebResponse)sessionRequest.GetResponse();

            if (sessionResponse.StatusCode == HttpStatusCode.OK)
            {
                using (System.IO.Stream sessionStream = sessionResponse.GetResponseStream())
                {
                    if (sessionStream != null)
                    {
                        using (System.IO.StreamReader sessionReader = new System.IO.StreamReader(sessionStream))
                        {
                            return sessionReader.ReadToEnd();
                        }
                    }

                    else
                    {
                        return "";
                    }
                }
            }

            else
            {
                return "";
            }
        }

        private void ExecuteService(int milliseconds)
        {
            _timer = new System.Timers.Timer();
            _timer.Enabled = true;
            _timer.Interval = milliseconds;
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Elapsed);
            _timer.Start();
        }
    }
}
