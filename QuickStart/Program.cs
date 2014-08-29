using System;
using System.Configuration;
using Microsoft.CSharp.RuntimeBinder;

using DataSift;
using DataSift.Enum;
using DataSift.Streaming;

namespace QuickStart
{
    class Program
    {
        // References we'll need to keep
        private static DataSiftStream _stream;
        private static string _hash;
        private static TestDbEntities _db;
        private static string _filter_keyword;

        static void Main(string[] args)
        {
            // Read settings from App.config
            _filter_keyword = ConfigurationManager.AppSettings.GetValues("datasift_keyword")[0];
            
            string datasift_username = ConfigurationManager.AppSettings.GetValues("datasift_username")[0];
            string datasift_apikey = ConfigurationManager.AppSettings.GetValues("datasift_apikey")[0];
            

            // Create a new DataSift client
            var client = new DataSiftClient(datasift_username, datasift_apikey);
          

            // Compile filter. {0} is replaced with _filter_keyword value
            var csdl = String.Format( @" return {   
                                                    twitter.text contains_any ""{0}"" 
                                                    AND 
                                                    (
                                                        twitter.user.time_zone == ""Kuala Lumpur""
                                                        OR
	                                                    twitter.user.time_zone == ""Asia/Kuala_Lumpur""
                                                    )
                                                 }", 
                                                   _filter_keyword);

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
            Console.WriteLine("KEYWORD:{0}. MESSAGE: {1}", _filter_keyword, message.twitter.text);


            #region GET TWEET SENTIMENT 
            string sentiment = null;
            try 
            {
                sentiment = Convert.ToString(message.salience.content.sentiment);
            }
            catch(RuntimeBinderException)
            {
                sentiment = null;
            }
            #endregion

            #region GET TWEET MESSAGE 
            var text = message.twitter.text;
            #endregion

            #region GET USER_ID, SCREEEN NAME 
            string user_id = null;
            try 
            {
                user_id = Convert.ToString(message.twitter.user.screen_name);
            }
            catch (RuntimeBinderException)
            {
                user_id = null;
            }
            #endregion

            #region GET LANGUAGE 
            var lang = message.twitter.lang;
            #endregion

            #region GET USER_LOCATION
            string user_location;
            try
            {
                user_location = Convert.ToString(message.twitter.user.location);
            }
            catch(RuntimeBinderException)
            {
                user_location = null;
            }
            #endregion

            #region GET USER_NAME 
            string user_name = null;
            try
            {
                user_name = Convert.ToString(message.twitter.user.name);
            }
            catch (RuntimeBinderException)
            {
                user_name = null;
            }
            #endregion

            #region GET COUNTRY NAME
            string country;
            try
            {
                country = Convert.ToString(message.twitter.place.country);
            }
            catch (RuntimeBinderException)
            {
                country = null;
            }
            #endregion

            #region GET REGION
            string region;
            try
            {
                region = Convert.ToString(message.twitter.place.full_name);
            }
            catch(RuntimeBinderException)
            {
                region = null;
            }

            #endregion

            #region GET TIME ZONE
            var time_zone = message.twitter.user.time_zone;
            #endregion

            #region GET GEO_LOCATION 
            string geo_latitude;
            string geo_longtitude;
            try
            {
                geo_latitude = Convert.ToString(message.twitter.geo.latitude);
                geo_longtitude = Convert.ToString(message.twitter.geo.longitude);
            }
            catch (RuntimeBinderException)
            {
                Console.WriteLine("NO GEO");
                geo_latitude = null;
                geo_longtitude = null;
            }
            #endregion

            #region GET DATE_TIME
            string date_time;
            try 
            {
                date_time = Convert.ToString(message.twitter.created_at);
            }
            catch (RuntimeBinderException)
            {
                date_time = null;
            }
            #endregion

            #region GET RETWEET COUNT
            string retweet_count;
            try
            {
                retweet_count = Convert.ToString(message.twitter.count);
            }
            catch (RuntimeBinderException)
            {
                retweet_count = null;
            }
            #endregion

            #region INSERT TO DATABASE
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
                Sentiment = sentiment,
                Date_Time = date_time,
                RetweetsCount = retweet_count,
                FilterKeyword = _filter_keyword
            };

            _db.SocialDatas.Add(msg);
            _db.Entry(msg).State = System.Data.Entity.EntityState.Added;

            _db.SaveChanges();
            #endregion

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
