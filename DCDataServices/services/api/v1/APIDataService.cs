using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DCDataServices.services.api.v1
{
    public class APIDataService : IDCDataService
    {
        #region vars
        private IWebHost host;
        #endregion //vars

        #region constructor
        public APIDataService ()
        {
            StartAPIServer();
        }
        #endregion //constructor

        #region util_funcs
        private void StartAPIServer()
        {
            //WebHost.CreateDefaultBuilder()
            //        .Configure(app => app.Run(c => c.Response.WriteAsync("Hello world!")))
            //        .Build().Run();
            //WebHost.Start(routes =>
            //    routes.MapGet("hello/{name}", (req, res, data) => res.WriteAsync($"Hello, {data.Values["name"]}")));
            var webhostbuilder = WebHost.CreateDefaultBuilder().Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e =>
                {
                    //e.MapGet("/", c => c.Response.WriteAsync("Hello world!"));
                    //e.MapGet("hello/{name}", c => c.Response.WriteAsync($"Hello, {c.Request.RouteValues["name"]}"));
                    e.MapGet("/", c => c.Response.WriteAsync(RootResponseStr()));
                    e.MapGet("hello/{name}", c => c.Response.WriteAsync(ResponseStrWithName(c.Request.RouteValues["name"].ToString() ) ) );
                });
            });
            host = webhostbuilder.Build();
            host.Start();
        }

        private void StopAPIServer()
        {
            if (host != null)
            {
                Task t = host.StopAsync();
                t.Wait();
                host = null;
            }
        }

        private string RootResponseStr()
        {
            return "in root response function";
        }

        private string ResponseStrWithName(string user_name)
        {
            return $"Helloooooo.. Good Morning... {user_name}";
        }

        #endregion //util_funcs

        #region interface_funcs
        public void PushData(string message)
        {
            throw new NotImplementedException();
        }
        #endregion // interface_funcs


    }
}
