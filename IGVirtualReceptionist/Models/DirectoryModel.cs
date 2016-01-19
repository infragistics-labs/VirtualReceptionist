using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BambooHR;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;

namespace IGVirtualReceptionist.Models
{
    public class DirectoryModel
    {
        private List<DirectoryEntryModel> Directory = null;
        public DirectoryModel()
        {

            //Company subdomain
            BambooAPIClient bambooClient = new BambooAPIClient("Infragistics");
            //Api Key, shush it is secret
            bambooClient.setSecretKey("SECRET");

            //Setup the fields to be requested
            string[] fields = { "firstName", "lastName", "jobTitle", "WorkEmail", "department", "division", "location", "supervisor", "nickname", "mobilePhone", "workPhone", "workPhoneExtension", "sms\text","id" };

            //Create the directroy so it is ready to fill
            Directory = new List<DirectoryEntryModel>();

            BambooHTTPResponse resp;
            //Get string of employees
            resp = bambooClient.getEmployeesReport("csv", "Report", fields);
            string list = resp.getContentString();

            //Remove escape characters/sequance from the string
            list = list.Replace("\"", "");
            //Split the string by employees
            char[] splitEmployees = { '\n' };
            string[] employees = list.Split(splitEmployees);
            for (int i = 1; i < employees.Length - 1; i++)
            {

                //Split out the fields by comma "," as long as it is not follow by a space
                string[] splitDetails = { ",(?!\\s)" };
                string[] employee = Regex.Split(employees[i], ",(?!\\s)");
                DirectoryEntryModel entry = new DirectoryEntryModel();
                entry.FirstName = employee[0];
                entry.LastName = employee[1];
                entry.Title = employee[2];
                entry.WorkEmail = employee[3];
                entry.Department = employee[4];
                entry.Division = employee[5];
                entry.Location = employee[6];
                entry.ReportingTo = employee[7];
                entry.Nickname = employee[8];
                entry.CellPhone = employee[9];
                entry.WorkPhone = employee[10];
                entry.WorkExt = employee[11];
                entry.SMS = false;
                entry.ID = int.Parse(employee[12]);

                entry.Photo = GetImage(bambooClient.baseUrl, entry);

                //Add the entry to the directory
                Directory.Add(entry);
            }

            Directory.Sort(new DirectoryEntryModelComparer());
 
        }
        public BitmapImage GetImage(string baseURL, DirectoryEntryModel entry)
        {
            BitmapImage retVal = new BitmapImage();
            string hashedEmail = GetMd5Hash(entry.WorkEmail);

            //// throws an exception about not having permissions from the server
            //string fullURL = string.Format("{0}/v1/employees/{1}/photo/small", baseURL, entry.ID);

            string fullURL = string.Format("https://infragistics.bamboohr.com/employees/photos/?h={0}", hashedEmail);

            WebClient web = new WebClient();
            
            byte[] imageData = web.DownloadData(fullURL);
            MemoryStream stream = new MemoryStream(imageData);
            retVal.BeginInit();
            retVal.StreamSource = stream;
            retVal.CacheOption = BitmapCacheOption.OnLoad;
            retVal.EndInit();
            
            stream.Close();

           
            return retVal;
        }

        static string GetMd5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        public List<DirectoryEntryModel> GetDirectory()
        {
            return Directory;
        }

        public List<DirectoryEntryModel> GetDataRefreshed()
        {
            //Company subdomain
            BambooAPIClient bambooClient = new BambooAPIClient("Infragistics");
            //Api Key, shush it is secret
            bambooClient.setSecretKey("SECRET");

            //Setup the fields to be requested
            string[] fields = { "firstName", "lastName", "jobTitle", "WorkEmail", "department", "division", "location", "supervisor", "nickname", "mobilePhone", "workPhone", "workPhoneExtension", "sms\text" };

            //Clear the directroy to read it for re-fill
            Directory.Clear();

            BambooHTTPResponse resp;
            //Get string of employees
            resp = bambooClient.getEmployeesReport("csv", "Report", fields);
            string list = resp.getContentString();
            
            //Remove escape characters/sequance from the string
            list = list.Replace("\"", "");
            //Split the string by employees
            char[] splitEmployees = { '\n' };
            string[] employees = list.Split(splitEmployees);
            for (int i = 1; i < employees.Length - 1; i++)
            {

                //Split out the fields by comma "," as long as it is not follow by a space
                string[] splitDetails = { ",(?!\\s)" };
                string[] employee = Regex.Split(employees[i], ",(?!\\s)");
                DirectoryEntryModel entry = new DirectoryEntryModel();
                entry.FirstName = employee[0];
                entry.LastName = employee[1];
                entry.Title = employee[2];
                entry.WorkEmail = employee[3];
                entry.Department = employee[4];
                entry.Division = employee[5];
                entry.Location = employee[6];
                entry.ReportingTo = employee[7];
                entry.Nickname = employee[8];
                entry.CellPhone = employee[9];
                entry.WorkPhone = employee[10];
                entry.WorkExt = employee[11];
                entry.SMS = false;

                //Add the entry to the directory
                Directory.Add(entry);
            }

            return Directory;
        }

        public DirectoryEntryModel GetHRContact()
        {
            //Hardcoded :(
            //DirectoryEntryModel hr = Directory.Find(x => x.LastName == "Gager");
            DirectoryEntryModel hr = Directory.Find(x => x.LastName == "Shea" & x.FirstName == "Christopher");
            if (hr.WorkExt == "")
            {
                hr.WorkExt = "1127";
            }

            return hr;
        }

        public DirectoryEntryModel GetSalesContact()
        {
            //Hardcoded :(
            //DirectoryEntryModel sales = Directory.Find(x => x.LastName == "Liston");
            DirectoryEntryModel sales = Directory.Find(x => x.LastName == "Shea" & x.FirstName == "Christopher");
            if (sales.WorkExt == "")
            {
                sales.WorkExt = "1203";
            }

            return sales;
        }

        public DirectoryEntryModel GetAccountingContact()
        {
            //Hardcoded :(
            //DirectoryEntryModel accouting = new DirectoryEntryModel();
            DirectoryEntryModel accouting = Directory.Find(x => x.LastName == "Shea" & x.FirstName == "Christopher");
            accouting.WorkExt = "1155";
            //accouting.FirstName = "";
            //accouting.LastName = "";
            //accouting.Department = "Accouting";

            return accouting;
        }
                
        public DirectoryEntryModel GetEntryByEmail(string id)
        {
            DirectoryEntryModel entry = Directory.Find((x => x.WorkEmail.ToLower() == id.ToLower()));
            return entry;
        }
    }
}
