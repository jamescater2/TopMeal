using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace TopMealApi.Context
{
    public class ContextUtils
    {
        public static string MealHelpStr =>
            "GET api/meal\n" +
            "GET api/meal/{filter}\n" +
            "GET api/meal/user/{id}\n" +
            "GET api/meal/user/{id}/{filter}\n" +
            "GET api/meal/remaining\n" +
            "GET api/meal/remaining/{userId}\n" +
            "GET api/meal/help\n" +
            "POST api/meal/{description}\n" +
            "POST api/meal/{calories}\n" +
            "POST api/meal/{calories}/{description}\n" +
            "POST api/meal [Meal]" +
            "PUT api/meal/{id}/{calories}\n" +
            "PUT api/meal/{id} [Meal]\n" +
            "DELETE api/meal/{id}\n" +
            "DELETE api/meal/alluser/{id}";

        public static string UserHelpStr =>
            "GET api/user\n" +
            "GET api/user/{filter}\n" +
            "GET api/user/{id}\n" + 
            "GET api/user/{id}/{filter}" +
            "GET api/user/help\n" +
            "POST api/user/{alias}\n" +
            "POST api/user [User]\n" +
            "PUT api/user/{id}/{role}\n" +
            "PUT api/user/{id} [User]\n" +
            "DELETE api/user/{id}";

        public static string RoleHelpStr =>
            "GET api/role\n" +
            "GET api/role/{id}\n" + 
            "GET api/role/{filter}\n" +
            "GET api/role/help\n" +
            "POST api/role/{name}\n" +
            "POST api/user [Role]\n" +
            "PUT api/role/{id}/{name}\n" +
            "PUT api/role/{id} [Role]\n" +
            "DELETE api/user/{id}";

        public static string ConvertFilterToLinq(string filter)
        {
            var ret = filter;

            if (ret != null)
            {
                ret = filter.Trim()
                    .Replace(" lt ", " < ")
                    .Replace(" gt ", " > ")
                    .Replace(" ge ", " >= ")
                    .Replace(" le ", " <= ")
                    .Replace(" eq ", " = ")
                    .Replace(" ne ", " != ")
                    .Replace(" && ", " AND ")
                    .Replace(" || ", " OR ");
            }
            return ret;
        }

        public static string[] RoleParameterToStringArray(string rolesStr) => (rolesStr ?? "").Split(',').Select(s => s.Trim()).ToArray();
    }

    public static class DateTimeUtils
    {
        public static bool IsDefault(this DateTime date) => date == default(DateTime);
        public static bool NotDefault(this DateTime date) => date != default(DateTime);

        public static DateTime TruncToSecond(this DateTime date) => date.Truncate(TimeSpan.TicksPerSecond);

        /// <summary>
        /// <para>Truncates a DateTime to a specified resolution.</para>
        /// <para>A convenient source for resolution is TimeSpan.TicksPerXXXX constants.</para>
        /// </summary>
        /// <param name="date">The DateTime object to truncate</param>
        /// <param name="resolution">e.g. to round to nearest second, TimeSpan.TicksPerSecond</param>
        /// <returns>Truncated DateTime</returns>
        public static DateTime Truncate(this DateTime date, long resolution) => new DateTime(date.Ticks - (date.Ticks % resolution), date.Kind);
    }

    static class HttpContextUtils
    {
        /// <summary>
        /// <para>Extracts User name from JWT Bearer Token</para>
        /// </summary>
        /// <param name="context">The HttpContext</param>
        /// <param name="unitTest">Allow unit tests to work without tokens</param>
        /// <returns>User Name</returns>
        public static string BearerUserName(this HttpContext context, string testUserName = null)
        {
            string ret = null;

            if (context != null && context.Request != null)
            {
                var authToken = context.Request.Headers["Authorization"];

                if (authToken.Count > 0 && authToken[0].StartsWith("Bearer "))
                {
                    var bearerToken = authToken[0].Substring("Bearer ".Length);
                    var userClaim = new JwtSecurityTokenHandler().ReadJwtToken(bearerToken).Claims.FirstOrDefault(c => c.Type == "unique_name");

                    if (userClaim != null)
                    {
                        ret = userClaim.Value;
                    }
                }
            }
            return ret == null && testUserName != null ? testUserName : ret;
        }
    }
}