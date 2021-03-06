﻿//using BtcE.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BtceApi
{
    class WebApi
    {
        public static string Query(string url)
        {
            var request = WebRequest.Create(url);
            request.Proxy = WebRequest.DefaultWebProxy;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            if (request == null)
                throw new Exception("Non HTTP WebRequest");
            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }
    }

    public class BtceApix
    {
        string key;
        HMACSHA512 hashMaker;
        UInt32 nonce;
        public BtceApix(string key, string secret)
        {
            this.key = key;
            hashMaker = new HMACSHA512(Encoding.ASCII.GetBytes(secret));
            nonce = UnixTime.Now;
        }
        public UserInfo GetInfo()
        {
            var resultStr = Query(new Dictionary<string, string>()
            {
                { "method", "getInfo" }
            });
            var result = JObject.Parse(resultStr);
            if (result.Value<int>("success") == 0)
                throw new Exception(result.Value<string>("error"));
            return UserInfo.ReadFromJObject(result["return"] as JObject);
        }

        public CouponResult RedeemCoupon(string coupon)
        {
            var args = new Dictionary<string, string>()
            {
                { "method", "RedeemCoupon" }
            };
            args.Add("coupon", coupon);
            var result = JObject.Parse(Query(args));
            if (result.Value<int>("success") == 0)
                throw new Exception(result.Value<string>("error"));
            return CouponResult.ReadFromJObject(result["return"] as JObject);
        }

        public CouponCreate CreateCoupon(string currency, int amount)
        {
            var args = new Dictionary<string, string>()
            {
                { "method", "CreateCoupon" }
            };
            args.Add("currency", currency);
            args.Add("amount", amount.ToString());
            var result = JObject.Parse(Query(args));
            if (result.Value<int>("success") == 0)
                throw new Exception(result.Value<string>("error"));
            return CouponCreate.ReadFromJObject(result["return"] as JObject);
        }

        string Query(Dictionary<string, string> args)
        {
            args.Add("nonce", GetNonce().ToString());

            var dataStr = BuildPostData(args);
            var data = Encoding.ASCII.GetBytes(dataStr);

            var request = WebRequest.Create(new Uri("https://btc-e.com/tapi")) as HttpWebRequest;
            if (request == null)
                throw new Exception("Non HTTP WebRequest");

            request.Method = "POST";
            request.Timeout = 15000;
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            request.Headers.Add("Key", key);
            request.Headers.Add("Sign", ByteArrayToString(hashMaker.ComputeHash(data)).ToLower());
            var reqStream = request.GetRequestStream();
            reqStream.Write(data, 0, data.Length);
            reqStream.Close();
            return new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();
        }
        static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }
        static string BuildPostData(Dictionary<string, string> d)
        {
            StringBuilder s = new StringBuilder();
            foreach (var item in d)
            {
                s.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode(item.Value));
                s.Append("&");
            }
            if (s.Length > 0) s.Remove(s.Length - 1, 1);
            return s.ToString();
        }

        UInt32 GetNonce()
        {
            return nonce++;
        }
        static string DecimalToString(decimal d)
        {
            return d.ToString(CultureInfo.InvariantCulture);
        }
        public static Depth GetDepth(BtcePair pair)
        {
            string queryStr = string.Format("https://btc-e.com/api/2/{0}/depth", BtcePairHelper.ToString(pair));
            return Depth.ReadFromJObject(JObject.Parse(WebApi.Query(queryStr)));
        }
        public static Ticker GetTicker(BtcePair pair)
        {
            string queryStr = string.Format("https://btc-e.com/api/2/{0}/ticker", BtcePairHelper.ToString(pair));
            return Ticker.ReadFromJObject(JObject.Parse(WebApi.Query(queryStr))["ticker"] as JObject);
        }
        public static List<TradeInfo> GetTrades(BtcePair pair)
        {
            string queryStr = string.Format("https://btc-e.com/api/2/{0}/trades", BtcePairHelper.ToString(pair));
            return JArray.Parse(WebApi.Query(queryStr)).OfType<JObject>().Select(TradeInfo.ReadFromJObject).ToList();
        }
        public static decimal GetFee(BtcePair pair)
        {
            string queryStr = string.Format("https://btc-e.com/api/2/{0}/fee", BtcePairHelper.ToString(pair));
            return JObject.Parse(WebApi.Query(queryStr)).Value<decimal>("trade");
        }

    }
}
