using System;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace KeyAuth
{
    public class api
    {
        public string name = "AppName";
        public string ownerid = "OwnerID";
        public string secret = "YourSecret";
        public string version = "1.0";

        public static bool IsAuthenticated = false;

        private static string sessionid;
        private static bool initialized;

        public api() { }

        [DataContract]
        private class response_structure
        {
            [DataMember] public bool success { get; set; }
            [DataMember] public string sessionid { get; set; }
            [DataMember] public string message { get; set; }
        }

        public void init()
        {
            if (initialized) return;
            string enckey = checksum(Guid.NewGuid().ToString());
            var values = new NameValueCollection
            {
                ["type"] = "init",
                ["ver"] = version,
                ["hash"] = checksum(Process.GetCurrentProcess().MainModule.FileName),
                ["enckey"] = enckey,
                ["name"] = name,
                ["ownerid"] = ownerid
            };
            var response = req(values);
            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.success) { sessionid = json.sessionid; initialized = true; }
            else { api.error(json.message); }
        }

        public void license(string key)
        {
            if (!initialized) init();
            var values = new NameValueCollection
            {
                ["type"] = "license",
                ["key"] = key,
                ["hwid"] = WindowsIdentity.GetCurrent().User.Value,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };
            var response = req(values);
            var json = response_decoder.string_to_generic<response_structure>(response);
            if (json.success) { IsAuthenticated = true; }
            else { this.response.message = json.message; }
        }

        public static string checksum(string filename)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(filename));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void error(string message) { Console.WriteLine("Error: " + message); Thread.Sleep(2000); Environment.Exit(0); }

        private static string req(NameValueCollection post_data)
        {
            using (WebClient client = new WebClient())
            {
                var raw = client.UploadValues("https://keyauth.win/api/1.2/", post_data);
                return Encoding.Default.GetString(raw);
            }
        }

        public response_class response = new response_class();
        public class response_class { public string message { get; set; } }
        private json_wrapper response_decoder = new json_wrapper(new response_structure());
    }

    public class json_wrapper
    {
        private DataContractJsonSerializer serializer;
        public json_wrapper(object obj) => serializer = new DataContractJsonSerializer(obj.GetType());
        public T string_to_generic<T>(string json) { using (var ms = new MemoryStream(Encoding.Default.GetBytes(json))) return (T)serializer.ReadObject(ms); }
    }
}
