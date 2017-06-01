using ADProvisioning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace ADProvisioning
{
    public class ADProvisioningService : IADProvisioningService
    {
        log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        string Hostname = System.Configuration.ConfigurationManager.ConnectionStrings["Hostname"].ConnectionString;
        string Username = System.Configuration.ConfigurationManager.ConnectionStrings["Username"].ConnectionString;
        string Password = System.Configuration.ConfigurationManager.ConnectionStrings["Password"].ConnectionString;
        string BaseDN = System.Configuration.ConfigurationManager.ConnectionStrings["BaseDN"].ConnectionString;
        string Path = "LDAP://" + System.Configuration.ConfigurationManager.ConnectionStrings["Hostname"].ConnectionString + "/" + System.Configuration.ConfigurationManager.ConnectionStrings["BaseDN"].ConnectionString;
        string TopDN = System.Configuration.ConfigurationManager.ConnectionStrings["TopDN"].ConnectionString;

        /// <summary>
        /// Creates user in AD
        /// </summary>
        /// <param name="person">Object to be created in AD</param>
        /// <returns>Success or error message</returns>
		public string Create(Person person)
        {
            if (VerifyToken(GetHeadertoken()).Equals("Authorized"))
            {
                String message = "201: User Created Successfully!";
                if (Search(person) != null)
                {
                    log.Error("User '" + person.Email + "' already exist in AD.");
                    return "415: User already Exist!";
                }
                else
                {
                    try
                    {
                        CreateUser(person);
                    }
                    catch (Exception e)
                    {
                        message = "Error:" + e.Message;
                        log.Error("Error while creating user '" + person.Email + "' " + e.StackTrace);
                    }
                    log.Info("User '" + person.Email + "' Created successfully!");
                }
                return message;
            }
            else
            {
                return "Unauthorised";
            }
        }

        /// <summary>
        /// Search if user present in AD
        /// </summary>
        /// <param name="person">User object to search</param>
        /// <returns>Status code for success or error</returns>
		public DirectoryEntry Search(Person person)
        {
            DirectoryEntry baseEntry = new DirectoryEntry(Path, Username, GetDecodedPassword(Password));
            try
            {
                if (person.Email == null)
                {
                    log.Error("Exception in search user. User email is null");
                    return null;
                }
                PrincipalContext adContext = EstablishConnection();
                if (adContext != null)
                {
                    using (adContext)
                    {
                        DirectorySearcher dirSearcher = new DirectorySearcher(baseEntry);
                        dirSearcher.Filter = "(&(objectClass=person))";
                        dirSearcher.SearchScope = SearchScope.Subtree;

                        SearchResultCollection results = dirSearcher.FindAll();

                        for (int i = 0; i < results.Count; i++)
                        {
                            DirectoryEntry entry = results[i].GetDirectoryEntry();
                            String mailValue = (string)entry.Properties["mail"].Value;
                            if ((mailValue != null) && mailValue.Equals(person.Email))
                            {
                                log.Info("User found in AD '" + person.Email + "'");
                                return entry;
                            }
                        }
                        log.Info("User not found in AD '" + person.Email + "'");
                        return null;
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                log.Info("Exception in search user '" + person.Email + "' " + e.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Update user in AD
        /// </summary>
        /// <param name="update">Update object contains email, property and value to update</param>
        /// <returns>success or error</returns>
        public string Update(UpdateInfo update)
        {
            if (VerifyToken(GetHeadertoken()).Equals("Authorized"))
            {
                PrincipalContext adContext = EstablishConnection();
                try
                {
                    if (adContext != null)
                    {
                        try
                        {
                            PrincipalSearcher searcher = new PrincipalSearcher();
                            UserPrincipal findUser = new UserPrincipal(adContext);
                            findUser.EmailAddress = update.Email;
                            searcher.QueryFilter = findUser;
                            UserPrincipal foundUser = (UserPrincipal)searcher.FindOne();
                            if (foundUser != null)
                            {
                                using (DirectoryEntry baseEntry = foundUser.GetUnderlyingObject() as DirectoryEntry)
                                {
                                    using (DirectoryEntry entry = new DirectoryEntry(baseEntry.Path, baseEntry.Username, GetDecodedPassword(System.Configuration.ConfigurationManager.ConnectionStrings["Password"].ConnectionString)))
                                    {
                                        try
                                        {
                                            string PropertyToUpdate = System.Configuration.ConfigurationManager.AppSettings[update.Property];
                                            if (update.Property.Equals("Member_Of"))
                                            {
                                                Person p = new Person();
                                                p.Email = update.Email;
                                                p.Member_Of = update.Value;
                                                manageMemberOfAttribute(p, GetDN(p.Email));
                                            }
                                            else if (update.Property.Equals("Direct_Reports"))
                                            {
                                                Person p = new Person();
                                                p.Email = update.Email;
                                                p.Direct_Reports = update.Value;
                                                manageDirectReports(p, GetDN(p.Email));
                                            }
                                            else if (PropertyToUpdate.Equals("manager"))
                                            {
                                                string managerDN = GetDN(update.Value);
                                                if (managerDN != null)
                                                {
                                                    entry.Properties[PropertyToUpdate].Value = managerDN;
                                                }
                                            }
                                            else if (PropertyToUpdate.Equals("AccountExpirationDate"))
                                            {
                                                DateTime Expirationdate = DateTime.ParseExact(update.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                                entry.Properties["accountExpires"].Value = Convert.ToString((Int64)Expirationdate.ToFileTime());
                                            }
                                            else
                                            {
                                                entry.Properties[PropertyToUpdate].Value = update.Value;
                                            }
                                            entry.CommitChanges();
                                            entry.Close();
                                            log.Info("User '" + update.Email + "' updated successfully");
                                            return "200: User updated successfully";
                                        }
                                        catch (Exception e)
                                        {
                                            log.Error("Exception in Update user: " + e.StackTrace);
                                            return "415: Please check property name and value. Then try again" + e.Message;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                log.Error("User not found in update '" + update.Email + "'");
                                return "404: User not found";
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error("Exception in Update user: " + e.StackTrace);
                            return e.Message;
                        }
                    }
                    else
                    {
                        log.Error("Exception in Update user. AD connection failed");
                        return "403: Error while connecting to AD";
                    }
                }
                catch (Exception e)
                {
                    log.Error("Exception in Update user: " + e.StackTrace);
                    return e.Message;
                }
            }
            else
            {
                return "Unauthorised";
            }
        }

        /// <summary>
        /// Deletes user from AD
        /// </summary>
        /// <param name="person">Person object to be deleted</param>
        /// <returns>Success or error</returns>
        public string Delete(Person person)
        {
            if (VerifyToken(GetHeadertoken()).Equals("Authorized"))
            {
                PrincipalContext adContext = EstablishConnection();
                try
                {
                    if (adContext != null)
                    {
                        PrincipalSearcher searcher = new PrincipalSearcher();
                        UserPrincipal deleteUser = new UserPrincipal(adContext);
                        deleteUser.EmailAddress = person.Email;
                        searcher.QueryFilter = deleteUser;
                        UserPrincipal foundUser = (UserPrincipal)searcher.FindOne();

                        if (foundUser != null)
                        {
                            try
                            {
                                foundUser.Delete();
                                log.Info("User '" + person.Email + "' deleted successfully");
                                return "200: User deleted successfully";
                            }
                            catch (Exception e)
                            {
                                log.Error("Exception in Delete user: " + e.StackTrace);
                                return "415: User deletion failed" + e.Message;
                            }
                        }
                        else
                        {
                            log.Error("User not found to delete '" + person.Email + "'");
                            return "404: User not found";
                        }
                    }
                    else
                    {
                        log.Error("Exception in Update user. AD connection failed");
                        return "403: Error while connecting to AD";
                    }
                }
                catch (Exception e)
                {
                    log.Error("Exception in Delete user: " + e.StackTrace);
                    return e.Message;
                }
            }
            else
            {
                return "Unauthorised";
            }
        }

        /// <summary>
        /// Disables user from AD
        /// </summary>
        /// <param name="person">Person object to disable</param>
        /// <returns>success or error</returns>
        public string Disable(Person person)
        {
            if (VerifyToken(GetHeadertoken()).Equals("Authorized"))
            {
                PrincipalContext adContext = EstablishConnection();
                try
                {
                    if (adContext != null)
                    {
                        PrincipalSearcher searcher = new PrincipalSearcher();
                        UserPrincipal deleteUser = new UserPrincipal(adContext);
                        deleteUser.EmailAddress = person.Email;
                        searcher.QueryFilter = deleteUser;
                        UserPrincipal foundUser = (UserPrincipal)searcher.FindOne();

                        if (foundUser != null)
                        {
                            if (foundUser.Enabled == true)
                            {
                                try
                                {
                                    foundUser.Enabled = false;
                                    foundUser.Save();
                                    log.Info("User '" + person.Email + "' disabled successfully");
                                    return "200: User disabled successfully";
                                }
                                catch (Exception e)
                                {
                                    log.Error("Exception in Disable user: " + e.StackTrace);
                                    return "415: User updating failed " + e.Message;
                                }
                            }
                            else
                            {
                                log.Error("User '" + person.Email + "' is already Disabled");
                                return "409: User is already Disabled";
                            }
                        }
                        else
                        {
                            log.Error("User '" + person.Email + "' not found in AD");
                            return "404: User not found";
                        }
                    }
                    else
                    {
                        log.Error("Exception in Disable user. AD connection failed");
                        return "403: Error while connecting to AD";
                    }
                }
                catch (Exception e)
                {
                    log.Error("Exception in Disable user: " + e.StackTrace);
                    return e.Message;
                }
            }
            else
            {
                return "Unauthorised";
            }
        }

        /// <summary>
        /// Establish connection to AD
        /// </summary>
        /// <returns>connection object</returns>
        private PrincipalContext EstablishConnection()
        {
            try
            {
                string DecodedPassword = GetDecodedPassword(Password);
                PrincipalContext adContext = new PrincipalContext(ContextType.Domain, Hostname, BaseDN, @Username, DecodedPassword);
                Boolean result = adContext.ValidateCredentials(Username, DecodedPassword);
                if (result)
                {
                    log.Info("Successfully Established connection to AD '" + Hostname + "' with username '" + Username + "'");
                    return adContext;
                }
                return null;
            }
            catch (Exception e)
            {
                log.Error("Exception in Establish Connection to AD '" + Hostname + "' with username '" + Username + "'" + e.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Get distinguished name of user
        /// </summary>
        /// <param name="Email">User's email</param>
        /// <returns>string containing DN of user</returns>
        private string GetDN(string Email)
        {
            try
            {
                PrincipalContext adContext = EstablishConnection();
                PrincipalSearcher searcher = new PrincipalSearcher();
                UserPrincipal findUser = new UserPrincipal(adContext);
                findUser.EmailAddress = Email;
                searcher.QueryFilter = findUser;
                UserPrincipal foundUser = (UserPrincipal)searcher.FindOne();
                if (foundUser != null)
                {
                    using (DirectoryEntry baseEntry = foundUser.GetUnderlyingObject() as DirectoryEntry)
                    {
                        using (DirectoryEntry entry = new DirectoryEntry(baseEntry.Path, baseEntry.Username, System.Configuration.ConfigurationManager.ConnectionStrings["Password"].ConnectionString))
                        {
                            return (string)entry.Properties["distinguishedName"].Value;
                        }
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                log.Error("Error while fetching DN of user '" + Email + "' from AD " + e.StackTrace);
                return e.Message;
            }
        }

        /// <summary>
        /// Test method to check WCF is working
        /// </summary>
        /// <returns>test string</returns>
        public string TestService()
        {
            try
            {
                log.Info("Test Service is running successfully!");
                return "Test Service working successfully!";
            }
            catch (Exception e)
            {
                log.Error("Exception in test service: " + e.StackTrace);
                return e.Message;
            }
        }

        /// <summary>
        /// get directory entry object for user
        /// </summary>
        /// <returns>directory entry object</returns>
		private DirectoryEntry getUsersDN()
        {
            try
            {
                DirectoryEntry usersEntry = new DirectoryEntry("LDAP://" + Hostname + "/" + BaseDN, Username, GetDecodedPassword(Password));
                usersEntry.RefreshCache();
                return usersEntry;
            }
            catch (Exception e)
            {
                log.Error("Error while fetching baseDN: " + e.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Get base DN directory entry object
        /// </summary>
        /// <returns>directory entry object</returns>
		private DirectoryEntry getTopDN()
        {
            try
            {
                DirectoryEntry usersEntry = new DirectoryEntry("LDAP://" + Hostname + "/" + TopDN, Username, GetDecodedPassword(Password));
                usersEntry.RefreshCache();
                return usersEntry;
            }
            catch (Exception e)
            {
                log.Error("Error while fetching baseDN: " + e.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Create new user in AD
        /// </summary>
        /// <param name="person">person object to be created</param>
		private void CreateUser(Person person)
        {
            DirectoryEntry rootEntry = getUsersDN();

            DirectoryEntry usersEntry = rootEntry.Children.Add("CN=" + person.First_Name, "user");
            usersEntry.Properties["sn"].Value = person.Last_Name;
            usersEntry.Properties["department"].Value = person.Department;
            usersEntry.Properties["telephoneNumber"].Value = person.Formatted_Phone;
            usersEntry.Properties["mail"].Value = person.Email;
            usersEntry.Properties["displayName"].Value = person.Display_Name;
            usersEntry.Properties["description"].Value = person.Description;
            usersEntry.Properties["physicalDeliveryOfficeName"].Value = person.Office;
            usersEntry.Properties["streetAddress"].Value = person.Street;
            usersEntry.Properties["l"].Value = person.City;
            usersEntry.Properties["postOfficeBox"].Value = person.PO_Box;
            usersEntry.Properties["title"].Value = person.Job_Title;
            usersEntry.Properties["company"].Value = person.Company;
            usersEntry.Properties["postalCode"].Value = person.Zip_Code;
            usersEntry.Properties["st"].Value = person.State;
            usersEntry.Properties["userPrincipalName"].Value = person.Email;


            string managerDN = GetDN(person.Manager);
            if (managerDN != null)
            {
                usersEntry.Properties["manager"].Value = managerDN;
            }
            else
            {
                log.Error("Error while adding Manager for user '" + person.Email + "'.");
            }
            usersEntry.Properties["employeeType"].Value = person.Employee_Type;
            usersEntry.Properties["employeeNumber"].Value = person.Employee_Number;

            if (person.Expiration_Time != null)
            {
                DateTime Expirationdate = DateTime.ParseExact(person.Expiration_Time, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                usersEntry.Properties["accountExpires"].Value = Convert.ToString((Int64)Expirationdate.ToFileTime());

            }
            else
            {
                log.Error("Expiration time not added for user '" + person.Email + "'.");
            }
            usersEntry.Properties["userAccountControl"].Value = 544;
            usersEntry.CommitChanges();
            if (person.Direct_Reports != null)
            {
                manageDirectReports(person, usersEntry.Properties["distinguishedName"].Value.ToString());
            }
            else
            {
                log.Error("Direct reports not added for user '" + person.Email + "'.");
            }

            if (person.Member_Of != null)
            {
                manageMemberOfAttribute(person, usersEntry.Properties["distinguishedName"].Value.ToString());
            }
            else
            {
                log.Error("memberOf not added for user '" + person.Email + "'.");
            }

        }

        /// <summary>
        /// Manage direct report object value
        /// </summary>
        /// <param name="person">person to be added to AD</param>
        /// <param name="userDN">DN of person to be added</param>
		private void manageDirectReports(Person person, String userDN)
        {
            String directReportsStr = person.Direct_Reports;
            String[] strArray = directReportsStr.Split(';');
            ArrayList list = new ArrayList();
            if ((strArray != null) && (strArray.Length > 0))
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    list.Add(strArray[i]);

                }
                searchEntryByEmail(list, userDN);
            }
        }

        /// <summary>
        /// adds direct reports for user
        /// </summary>
        /// <param name="list">list of direct reports</param>
        /// <param name="userDN">DN of person to be added</param>
		private void searchEntryByEmail(ArrayList list, String userDN)
        {
            try
            {
                ArrayList returnList = new ArrayList();
                DirectoryEntry de = getUsersDN();
                DirectorySearcher ds = new DirectorySearcher(de);
                ds.Filter = "(&(objectClass=person))";
                ds.SearchScope = SearchScope.Subtree;

                SearchResultCollection results = ds.FindAll();

                for (int i = 0; i < results.Count; i++)
                {
                    DirectoryEntry entry = results[i].GetDirectoryEntry();
                    String mailValue = (string)entry.Properties["mail"].Value;
                    if ((mailValue != null) && (list.Contains(mailValue)))
                    {
                        returnList.Add(entry.Properties["distinguishedName"].Value);
                        entry.Properties["manager"].Value = userDN;
                        entry.CommitChanges();
                        entry.RefreshCache();
                        log.Info("Successfully added direct reports for user");
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Exception occured while assigning direct reports attribute to user " + e.StackTrace);
            }
        }

        /// <summary>
        /// adds member of attribute
        /// </summary>
        /// <param name="person">person object</param>
        /// <param name="userDN">DN of person to be added</param>
		private void manageMemberOfAttribute(Person person, String userDN)
        {
            String memberOfString = person.Member_Of;
            String[] strArray = memberOfString.Split(';');
            ArrayList list = new ArrayList();
            if ((strArray != null) && (strArray.Length > 0))
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    list.Add(strArray[i]);
                }
                getGroupsDN(list, userDN);
            }

        }

        /// <summary>
        /// adds memberof attribute
        /// </summary>
        /// <param name="list">list of groups</param>
        /// <param name="userDN">DN of person to be added</param>
		private void getGroupsDN(ArrayList list, String userDN)
        {
            try
            {
                ArrayList returnList = new ArrayList();
                DirectoryEntry de = getTopDN();
                DirectorySearcher ds = new DirectorySearcher(de);
                ds.Filter = "(&(objectClass=group))";
                ds.SearchScope = SearchScope.Subtree;

                SearchResultCollection results = ds.FindAll();

                for (int i = 0; i < results.Count; i++)
                {
                    DirectoryEntry entry = results[i].GetDirectoryEntry();
                    String cnValue = (string)entry.Properties["cn"].Value;
                    if ((cnValue != null) && (list.Contains(cnValue)))
                    {
                        returnList.Add(entry.Properties["distinguishedName"].Value);
                        entry.Properties["member"].Add(userDN);
                        entry.CommitChanges();
                        entry.RefreshCache();
                        log.Info("Successfully added memberOf attributes for user ");
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Exception occured while assigning MemberOf attribute to user " + e.StackTrace);
            }
        }

       /// <summary>
       /// Method gets token from request header
       /// </summary>
       /// <returns></returns>
        private string GetHeadertoken()
        {
            HttpContext httpContext = HttpContext.Current;
            NameValueCollection headerList = httpContext.Request.Headers;
            return headerList.Get("Authorization");
        }

        private string GetDecodedPassword(string Encodedpassword)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(Encodedpassword);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /// <summary>
        /// Method generates token after verifying username and password
        /// </summary>
        /// <param name="creds"></param>
        /// <returns></returns>
        public string GenerateToken(UserCredentials creds)
        {
            string APIusername = System.Configuration.ConfigurationManager.ConnectionStrings["APIUsername"].ConnectionString;
            string APIpassword = System.Configuration.ConfigurationManager.ConnectionStrings["APIPassword"].ConnectionString;
          
            string decodedAPIpassword= GetDecodedPassword(APIpassword);
            if (creds.Username.Equals(APIusername) && creds.Password.Equals(decodedAPIpassword))
            {
                //Encode time after converting it to binary
                long TimeinLong = DateTime.UtcNow.ToBinary();
               
               var Timeinbytes = System.Text.Encoding.UTF8.GetBytes(Convert.ToString(TimeinLong));
               string time= System.Convert.ToBase64String(Timeinbytes);
                //convert encoded time to bytes
                byte[] Encodedtime = Encoding.ASCII.GetBytes(time);
                //concatnate with password
                byte[] key = Encoding.ASCII.GetBytes(decodedAPIpassword);
                byte[] token = key.Concat(Encodedtime).ToArray();
                //create token
                string hashtoken = Convert.ToBase64String(token);
                VerifyToken(hashtoken);
                return hashtoken;
            }
            else
            {
                return "Unauthorized! Wrong username or password";
            }
        }
        
        /// <summary>
        /// Method verifies token for authorization
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string VerifyToken(string token)
        {
            try
            {
                byte[] data = Convert.FromBase64String(token);
                string unhashedToken = ASCIIEncoding.ASCII.GetString(data);

                string APIpassword = System.Configuration.ConfigurationManager.ConnectionStrings["APIPassword"].ConnectionString;
            
                string decodedAPIpassword = GetDecodedPassword(APIpassword);

                int indexofkey = unhashedToken.IndexOf(decodedAPIpassword);
                if (indexofkey != -1)
                {
                    string timestamp = unhashedToken.Substring(indexofkey + decodedAPIpassword.Length);
                    //Decode encoded time
                    var Encodedtime = System.Convert.FromBase64String(timestamp);
                    string time = System.Text.Encoding.UTF8.GetString(Encodedtime);
                    //convert in long to get it as DateTime
                    long Timelong = Convert.ToInt64(time);
                    DateTime when = DateTime.FromBinary(Timelong);
                    if (when < DateTime.UtcNow.AddHours(-24))
                    {
                        return "Unauthorized! User Token Expired";
                    }
                    else
                    {
                        return "Authorized";
                    }
                }
                else
                {
                    return "Unauthorized";
                }
            }catch(Exception e)
            {
                log.Error("Exception occured while verifying token: "+e.StackTrace);
                return "Unauthorized";
            }
        }
    }
}