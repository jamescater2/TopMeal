using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TopMealApi.Utils
{
    public class NutritionixUtils
    {
        private const string _nf_cal_string = "\"nf_calories\":";

        public static int GetCaloriesInt(string xAppId, string xAppKey, string query)
        {
            Console.WriteLine($"# # # GetCaloriesInt({query})");
            var ret = NutritionixWebRequest(xAppId, xAppKey, query);
            return (int)Math.Round(ret);
        }

        public static async Task<int> GetCaloriesIntAsync(string xAppId, string xAppKey, string query)
        {
            Console.WriteLine($"# # # GetCaloriesInt({query})");
            var ret = await NutritionixWebRequestAsync(xAppId, xAppKey, query);
            return (int)Math.Round(ret);
        }

        public static double GetCaloriesDbl(string xAppId, string xAppKey, string query)
        {
            Console.WriteLine($"# # # GetCaloriesDbl({query})");
            var ret = NutritionixWebRequest(xAppId, xAppKey, query);

            return ret;
        }

        public static async Task<double> GetCaloriesDblAsync(string xAppId, string xAppKey, string query)
        {
            Console.WriteLine($"# # # GetCaloriesDbl({query})");
            var ret = await NutritionixWebRequestAsync(xAppId, xAppKey, query);
            return ret;
        }

        private static double NutritionixWebRequest(string xAppId, string xAppKey, string query)
        {
            var ret = 0.0;
            HttpWebResponse response;

            //Console.WriteLine("# # # # # NutritionixWebRequest 1 ");

            if (RequestNutritionixCom(xAppId, xAppKey, out response, query))
            {
                //Console.WriteLine("# # # # # NutritionixWebRequest 2 ");

                using (var dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    var reader = new StreamReader(dataStream, Encoding.GetEncoding(response.CharacterSet));
                    var respStr = reader.ReadToEnd();
                    var calIdxStart = respStr.IndexOf(_nf_cal_string);  // "nf_calories": 227.01,

                    //Console.WriteLine("# # # # # NutritionixWebRequest 3 " );
                    //Console.WriteLine(respStr);

                    while (calIdxStart > -1)
                    {
                        calIdxStart += _nf_cal_string.Length;
                        var calIdxEnd = respStr.IndexOf(',', calIdxStart);

                        // Console.WriteLine("# # # # # NutritionixWebRequest 4 ");

                        if (calIdxEnd > -1)
                        {
                            //var blah = respStr.AsSpan(calIdxStart,  calIdxEnd - calIdxStart);
                            //Console.WriteLine($"# # # # NutritionixWebRequest >{blah.ToString()}<");

                            if (double.TryParse(respStr.AsSpan(calIdxStart,  calIdxEnd - calIdxStart), out double val))
                            {
                                Console.WriteLine($"# # # # NutritionixWebRequest {val}");
                                ret += val;
                                calIdxStart = respStr.IndexOf(_nf_cal_string, calIdxEnd+1);
                            }
                            else
                            {
                                calIdxStart = -1;
                            }
                        }
                        else
                        {
                            calIdxStart = -1;
                        }
                    }
                }
                response.Close();
            }
            return ret;
        }

        private static async Task<double> NutritionixWebRequestAsync(string xAppId, string xAppKey, string query)
        {
            var ret = 0.0;
            var response = await RequestNutritionixComAsync(xAppId, xAppKey, query);

            //Console.WriteLine("# # # # # NutritionixWebRequest 1 ");

            if (response != null)
            {
                //Console.WriteLine("# # # # # NutritionixWebRequest 2 ");

                using (var dataStream = response.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    var reader = new StreamReader(dataStream, Encoding.GetEncoding(response.CharacterSet));
                    var respStr = reader.ReadToEnd();
                    var calIdxStart = respStr.IndexOf(_nf_cal_string);  // "nf_calories": 227.01,

                    //Console.WriteLine("# # # # # NutritionixWebRequest 3 " );
                    //Console.WriteLine(respStr);

                    while (calIdxStart > -1)
                    {
                        calIdxStart += _nf_cal_string.Length;
                        var calIdxEnd = respStr.IndexOf(',', calIdxStart);

                        // Console.WriteLine("# # # # # NutritionixWebRequest 4 ");

                        if (calIdxEnd > -1)
                        {
                            //var blah = respStr.AsSpan(calIdxStart,  calIdxEnd - calIdxStart);
                            //Console.WriteLine($"# # # # NutritionixWebRequest >{blah.ToString()}<");

                            if (double.TryParse(respStr.AsSpan(calIdxStart,  calIdxEnd - calIdxStart), out double val))
                            {
                                Console.WriteLine($"# # # # NutritionixWebRequest {val}");
                                ret += val;
                                calIdxStart = respStr.IndexOf(_nf_cal_string, calIdxEnd+1);
                            }
                            else
                            {
                                calIdxStart = -1;
                            }
                        }
                        else
                        {
                            calIdxStart = -1;
                        }
                    }
                }
                response.Close();
            }
            return ret;
        }

        private static bool RequestNutritionixCom(string xAppId, string xAppKey, out HttpWebResponse response, string query)
        {
            response = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://trackapi.nutritionix.com/v2/natural/nutrients/");

                //request.Headers.Add("x-app-id", @"62031f66");
                //request.Headers.Add("x-app-key", @"aa0049adb09f7397e00c630db7952cf6");
                request.Headers.Add("x-app-id", xAppId);
                request.Headers.Add("x-app-key", xAppKey);
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = "PostmanRuntime/7.15.2";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                // request.Headers.Add("Postman-Token", @"59ac910a-c33b-48fa-ac47-769747f75a61");
                // request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=df9d66b8b334aafb0f7b70e136c4fa9451566939997");
                request.AutomaticDecompression = DecompressionMethods.GZip;
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.KeepAlive = false;

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                var body = @"query=" + query;
                var postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                var stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                Console.WriteLine("# # # #     WebException ", e.Message);
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("# # # #     Exception ", e.Message);
                if(response != null) response.Close();
                return false;
            }
            return true;
        }

        private static async Task<HttpWebResponse> RequestNutritionixComAsync(string xAppId, string xAppKey, string query)
        {
            var response = (HttpWebResponse)null;
            
            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://trackapi.nutritionix.com/v2/natural/nutrients/");

                //request.Headers.Add("x-app-id", @"62031f66");
                //request.Headers.Add("x-app-key", @"aa0049adb09f7397e00c630db7952cf6");
                request.Headers.Add("x-app-id", xAppId);
                request.Headers.Add("x-app-key", xAppKey);
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = "PostmanRuntime/7.15.2";
                request.Accept = "*/*";
                request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                // request.Headers.Add("Postman-Token", @"59ac910a-c33b-48fa-ac47-769747f75a61");
                // request.Headers.Set(HttpRequestHeader.Cookie, @"__cfduid=df9d66b8b334aafb0f7b70e136c4fa9451566939997");
                request.AutomaticDecompression = DecompressionMethods.GZip;
                //request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.KeepAlive = false;

                request.Method = "POST";
                request.ServicePoint.Expect100Continue = false;

                var body = @"query=" + query;
                var postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                var stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                response = await request.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException e)
            {
                Console.WriteLine("# # # #     WebException ", e.Message);
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("# # # #     Exception ", e.Message);
                if(response != null) response.Close();
                return null;
            }
            return response;
        }
    }
}