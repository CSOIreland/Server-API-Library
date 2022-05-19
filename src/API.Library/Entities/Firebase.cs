using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace API
{
    public static class Firebase
    {
        //For reference: https://firebase.google.com/docs/admin/setup



        /// <summary>
        /// Returns a firebase user based on Access Token
        // You must import System.Collections.Immutable Version 1.7.1 from NuGet
        /// </summary>
        /// <returns></returns>
        public static bool Authenticate(string Uid, string AccessToken)
        {
            if (!Common.FirebaseEnabled) return false;

            try
            {
                if (Uid == null || AccessToken == null) return false;

                string configPath = HostingEnvironment.MapPath(@"~\Resources\") + "FirebaseKey.json";


                //Create the app if we need to
                if (FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance == null)
                {
                    try
                    {
                        var defaultApp = FirebaseApp.Create(new AppOptions()
                        {

                            Credential = GoogleCredential.FromFile(configPath)
                        });
                    }
                    //FirebaseApp already exists - continue
                    catch (System.ArgumentException)
                    {
                        Log.Instance.Error("Firebase app exists already - attempt to continue");
                        var defaultApp = FirebaseApp.GetInstance(ConfigurationManager.AppSettings["API_FIREBASE_APP_NAME"]);

                    }
                }

                //validate the token
                Task<FirebaseToken> tToken = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(AccessToken);
                tToken.Wait();

                if (tToken?.Result == null)
                    return false;

                return tToken.Result.Uid.Equals(Uid);


            }
            catch (Exception ex)
            {
                Log.Instance.Error("Error authenticating Firebase token: " + ex.Message + ": " + ex.GetType());
                return false;
            }
        }

        /// <summary>
        /// Get all of the Firebase users for this Firebase Project
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, dynamic> GetAllUsers()
        {
            string configPath = HostingEnvironment.MapPath(@"~\Resources\") + "FirebaseKey.json";

            //We use hash as an identifer for the cache. This is based on the contents of the firebaseKey file
            string hash;
            using (StreamReader sr = new StreamReader(configPath))
            {
                hash = Utility.GetSHA256(sr.ReadToEnd());
            }

            Dictionary<string, dynamic> userList = new Dictionary<string, dynamic>();

            //Create the app if we need to

            if (FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance == null)
            {
                try
                {
                    var defaultApp = FirebaseApp.Create(new AppOptions()
                    {

                        Credential = GoogleCredential.FromFile(configPath)
                    });
                }
                catch
                {
                    Log.Instance.Error("Firebase app exists already - attempt to continue");
                    var defaultApp = FirebaseApp.GetInstance(ConfigurationManager.AppSettings["API_FIREBASE_APP_NAME"]);
                }
            }

            //Read from Firebase
            var pagedEnumerable = FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.ListUsersAsync(new ListUsersOptions() { PageSize = 1000 });
            var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
            bool allRead = false;
            while (!allRead)
            {
                var wt = responses.MoveNextAsync();
                if (!wt.Result) break;
                ExportedUserRecords response = responses.Current;
                foreach (ExportedUserRecord user in response.Users)
                {

                    dynamic obj = new ExpandoObject();
                    obj.Uid = user.Uid;
                    if (user.ProviderData.Length > 0)
                    {
                        obj.Email = user.ProviderData[0].Email ?? user.Email;
                        obj.DisplayName = user.ProviderData[0].DisplayName ?? user.DisplayName;
                    }
                    else
                    {
                        obj.Email = user.Email;
                        obj.DisplayName = user.DisplayName;
                    }
                    userList.Add(user.Uid, obj);
                }
            }


            return userList;
        }



        /// <summary>
        /// Delete a user from Firebase
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static bool DeleteUser(string uid)
        {
            string configPath = HostingEnvironment.MapPath(@"~\Resources\") + "FirebaseKey.json";
            try
            {
                if (FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance == null)
                {
                    try
                    {
                        var defaultApp = FirebaseApp.Create(new AppOptions()
                        {

                            Credential = GoogleCredential.FromFile(configPath)
                        });
                    }
                    catch
                    {
                        Log.Instance.Error("Firebase app exists already - attempt to continue");
                        var defaultApp = FirebaseApp.GetInstance(ConfigurationManager.AppSettings["API_FIREBASE_APP_NAME"]);
                    }
                }
                var token = FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
                token.Wait();
                Log.Instance.Debug("Firebase user deleted: " + uid);
                return true;
            }
            catch (Exception ex)
            {
                Log.Instance.Debug("Can't delete Firebase user: " + uid + " Error: " + ex.Message);
                return false;
            }
        }
    }


}
