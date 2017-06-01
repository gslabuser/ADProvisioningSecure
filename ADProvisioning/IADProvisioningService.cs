using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Http;

namespace ADProvisioning
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IADProvisioningService
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                          RequestFormat = WebMessageFormat.Json,
                         UriTemplate = "/testservice")]
        string TestService();

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                           RequestFormat = WebMessageFormat.Json,
                          UriTemplate = "/login")]
        string GenerateToken(UserCredentials creds);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                           RequestFormat = WebMessageFormat.Json,
                          UriTemplate = "/createuser")]
        string Create(Person person);

        /*[OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                          RequestFormat = WebMessageFormat.Json,
                         UriTemplate = "/searchuser")]*/
        DirectoryEntry Search(Person person);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                          RequestFormat = WebMessageFormat.Json,
                         UriTemplate = "/updateuser")]
        string Update(UpdateInfo update);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                           RequestFormat = WebMessageFormat.Json,
                          UriTemplate = "/deleteuser")]
        string Delete(Person person);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Bare,
                           RequestFormat = WebMessageFormat.Json,
                          UriTemplate = "/disableuser")]
        string Disable(Person person);

    }

    }
