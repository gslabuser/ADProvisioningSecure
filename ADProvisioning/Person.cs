using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ADProvisioning
{
    [DataContract]
    public class Person
    {
        [DataMember]
        public string First_Name { get; set; }

        [DataMember]
        public string Last_Name { get; set; }

        [DataMember]
        public string Department { get; set; }

        [DataMember]
        public string Formatted_Phone { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string Display_Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Office { get; set; }

        [DataMember]
        public string Street { get; set; }

        [DataMember]
        public string PO_Box { get; set; }

        [DataMember]
        public string City { get; set; }

        [DataMember]
        public string State { get; set; }

        [DataMember]
        public string Zip_Code { get; set; }

        [DataMember]
        public string Job_Title { get; set; }

        [DataMember]
        public string Company { get; set; }

        [DataMember]
        public string Manager { get; set; }

        [DataMember]
        public string Direct_Reports { get; set; }

        [DataMember]
        public string Member_Of { get; set; }

        [DataMember]
        public string Employee_Type { get; set; }
        
        [DataMember]
        public string Employee_Number { get; set; }

        [DataMember]
        public string Empl_Type { get; set; }

        [DataMember]
        public string Expiration_Time { get; set; }

        [DataMember]
        public string Parent_OU { get; set; }

        [DataMember]
        public string SAM_Account_Name { get; set; }

    

    }

}