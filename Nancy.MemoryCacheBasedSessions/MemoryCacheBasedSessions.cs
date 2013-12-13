using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace Nancy.Session
{
    public class MemoryCacheBasedSessions
    {
        static private string SessionIdKey = "_ncs";

        public static void Enable(IPipelines pipelines)
        {
            var store = new MemoryCacheBasedSessions(MemoryCache.Default);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx => LoadSession(ctx, store));
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx => SaveSession(ctx, store));
        }

        private readonly ObjectCache _cache;

        public MemoryCacheBasedSessions(ObjectCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Save the session data to the memory cache, and set the session id cookie
        /// in the response
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="session"></param>
        /// <param name="response"></param>
        public void Save(string sessionId, ISession session, Response response)
        {
            var sess = session as Session;

            if (sess == null)
            {
                return;
            }

            var dict = new Dictionary<string, object>();

            foreach (var kvp in session)
            {
                dict[kvp.Key] = kvp.Value;
            }

            var cookie = new NancyCookie(SessionIdKey, sessionId);
            cookie.Expires = DateTime.UtcNow.AddMinutes(30);
            response.AddCookie(cookie);

            _cache.Set(sessionId, dict, DateTime.Now + TimeSpan.FromMinutes(30 + 1));
        }

        /// <summary>
        /// Load the session data from the nancy context 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public ISession Load(NancyContext context)
        {
            var request = context.Request;

            if (request.Cookies.ContainsKey(SessionIdKey) && _cache.Any(kvp => kvp.Key == request.Cookies[SessionIdKey]))
            {
                return new Session(_cache[request.Cookies[SessionIdKey]] as Dictionary<string, object>);
            }

            return new Session(new Dictionary<string, object>());
        }

        private static void SaveSession(NancyContext context, MemoryCacheBasedSessions sessionStore)
        {
            string sessionId;

            if (context.Request.Cookies.ContainsKey(SessionIdKey))
            {
                sessionId = context.Request.Cookies[SessionIdKey];
            }
            else
            {
                sessionId = Guid.NewGuid().ToString();
            }

            sessionStore.Save(sessionId, context.Request.Session, context.Response);
        }

        /// <summary>
        /// Loads the request session
        /// </summary>
        /// <param name="context">Nancy context</param>
        /// <param name="sessionStore">Session store</param>
        /// <returns>Always returns null</returns>
        private static Response LoadSession(NancyContext context, MemoryCacheBasedSessions sessionStore)
        {
            context.Request.Session = sessionStore.Load(context);

            return null;
        }
    }
}
