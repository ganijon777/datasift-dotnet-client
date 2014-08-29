using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataSift;
using DataSift.Enum;
using DataSift.Streaming;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Microsoft.CSharp.RuntimeBinder;           
using System.Configuration;

namespace QuickStart
{
    class Program
    {
        // References we'll need to keep
        private static DataSiftStream _stream;
        private static string _hash;
        private static TestDbEntities _db = new TestDbEntities();
        private static string filter_keyword;

        static void Main(string[] args)
        {
            string datasift_username = ConfigurationManager.AppSettings.GetValues("datasift_username")[0];
            string datasift_apikey = ConfigurationManager.AppSettings.GetValues("datasift_apikey")[0];
            string datasift_keyword = ConfigurationManager.AppSettings.GetValues("datasift_keyword")[0];
            // Create a new DataSift client
            var client = new DataSiftClient(datasift_username, datasift_apikey);
            filter_keyword = datasift_keyword;
          

            // Compile filter
            var csdl = " return { twitter.text contains_any \"" + datasift_keyword +"\" ";
            //var csdl = " return {";
            csdl += @"AND 
                        (twitter.user.time_zone == ""Kuala Lumpur""
                    OR
	                    twitter.user.time_zone == ""Asia/Kuala_Lumpur"")
                    }";
            

            var compiled = client.Compile(csdl);
            _hash = compiled.Data.hash;

            _stream = client.Connect();
            _stream.OnConnect += stream_OnConnect;
            _stream.OnMessage += stream_OnMessage;
            _stream.OnDelete += stream_OnDelete;
            _stream.OnDataSiftMessage += stream_OnDataSiftMessage;
            _stream.OnClosed += stream_OnClosed;

            // Wait for key press before ending example
            Console.WriteLine("-- Press any key to exit --");
            Console.ReadKey(true);

        }

        static void stream_OnConnect()
        {
            Console.WriteLine("Connected to DataSift.");
            // Subscribe to stream
            _stream.Subscribe(_hash);
        }

        static void stream_OnMessage(string hash, dynamic message)
        {
            string filter_keywords_on = filter_keyword;
            Console.WriteLine("{0}", message.twitter.text);
            string sentimental = null;
            try 
            {
                sentimental = Convert.ToString(message.salience.content.sentiment);
            }
            catch(RuntimeBinderException)
            {
                sentimental = null;
            }
            //GET TWEET MESSAGE FROM TWITTER================================================
            var text = message.twitter.text;

            //GET USER_ID, SCREEEN NAME FROM TWITTER=========================================
            string user_id = null;
            try 
            {
                user_id = Convert.ToString(message.twitter.user.screen_name);
            }
            catch (RuntimeBinderException)
            {
                user_id = null;
            }
            //===============================================================================
           
            var lang = message.twitter.lang;
            //==============================================================================

            //GET USER_LOCATION TWITTER======================================================
            string user_location;
            try
            {
                user_location = Convert.ToString(message.twitter.user.location);
            }
            catch(RuntimeBinderException)
            {
                user_location = null;
            }
           //=================================================================================

            //GET USER NAME FROM TWITTER======================================================
            string user_name = null;
            try
            {
                user_name = Convert.ToString(message.twitter.user.name);
            }
            catch (RuntimeBinderException)
            {
                user_name = null;
            }
            //================================================================================
            
            
            // GET COUNTRY NAME FROM TWITTER==================================================
            string country;
            try
            {
                country = Convert.ToString(message.twitter.place.country);
            }
            catch (RuntimeBinderException)
            {
                country = null;
            }
            //=================================================================================

            //GET REGION FROM TWITTER==========================================================
            string region;
            try
            {
                region = Convert.ToString(message.twitter.place.full_name);
            }
            catch(RuntimeBinderException)
            {
                region = null;
            }

            //=================================================================================
            var time_zone = message.twitter.user.time_zone;
            
            //GET GEO_LOCATION FROM TWITTER
            string geo_latitude;
            string geo_longtitude;
            //dynamic geo = message.twitter.geo;
            try
            {
                //Console.WriteLine("{0}", Convert.ToString(message.twitter.geo));
                geo_latitude = Convert.ToString(message.twitter.geo.latitude);
                geo_longtitude = Convert.ToString(message.twitter.geo.longitude);
            }
            catch (RuntimeBinderException)
            {
                Console.WriteLine("NO GEO");
                geo_latitude = null;
                geo_longtitude = null;
            }
            //====================================================================================

            //GET DATE_TIME FROM TWITTER
            string date_time;
            try {
                date_time = Convert.ToString(message.twitter.created_at);
            }
            catch (RuntimeBinderException)
            {
                date_time = null;
            }
            //========================================================================================

            //GET RETWEET COUNT=======================================================================
            string retweet_count;
            try
            {
                retweet_count = Convert.ToString(message.twitter.count);
            }
            catch (RuntimeBinderException)
            {
                retweet_count = null;
            }
            

            //========================================================================================
            var msg = new SocialDatas
            {
                Message = text,
                User_Id = user_id,
                Language = lang,
                User_Location = user_location,
                Place_Country = country,
                Place_Region = region,
                User_Time_Zone = time_zone,
                User_Name = user_name,
                Geo_Latitude = geo_latitude,
                Geo_Longitude = geo_longtitude,
                Sentiment = sentimental,
                Date_Time = date_time,
                RetweetsCount = retweet_count,
                FilterKeyword = filter_keywords_on

            };

            _db.SocialDatas.Add(msg);
             _db.Entry(msg).State = System.Data.Entity.EntityState.Added;

            _db.SaveChanges();


        }
        public static bool PropertyExist(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName) != null;
        }
        static void stream_OnDelete(string hash, dynamic message)
        {
            // You must delete the interaction to stay compliant
            Console.WriteLine("Deleted: {0}", message.interaction.id);
        }

        static void stream_OnDataSiftMessage(DataSift.Enum.DataSiftMessageStatus status, string message)
        {
            switch (status)
            {
                case DataSiftMessageStatus.Warning:
                    Console.WriteLine("WARNING: " + message);
                    break;
                case DataSiftMessageStatus.Failure:
                    Console.WriteLine("FAILURE: " + message);
                    break;
                case DataSiftMessageStatus.Success:
                    Console.WriteLine("SUCCESS: " + message);
                    break;
            }
        }

        static void stream_OnClosed()
        {
            Console.WriteLine("Connection has been closed.");
        }
    }
}
