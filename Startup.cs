using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GitterAlertForwarder
{
    public class Startup
    {
        
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).Build().Run();
        }
        
        
        
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                var httpClient = new HttpClient();
                var gitterRoomUrl = Environment.GetEnvironmentVariable("GITTER_ROOM_URL");
                var gitterToken = Environment.GetEnvironmentVariable("GITTER_TOKEN");
                    
                endpoints.MapPost("/", async context =>
                {
                    using var streamReader = new StreamReader(context.Request.Body);
                    var json = await streamReader.ReadToEndAsync();

                    await ForwardToGitterMessage(httpClient, json, gitterRoomUrl, gitterToken);
                    context.Response.StatusCode = 200;
                });
            });
        }

        private async Task ForwardToGitterMessage(HttpClient httpClient, string json, string gitterRoomUrl, string gitterToken)
        {
            Console.WriteLine("收到告警信息如下：");
            Console.WriteLine("======================");
            Console.WriteLine(json);
            var alert = JsonSerializer.Deserialize<AlertMessage>(json).alerts.FirstOrDefault();

            var status = alert.status;
            var summary = alert.annotations?.FirstOrDefault(item => item.Key == "summary");
            var description = alert.annotations?.FirstOrDefault(item => item.Key == "description");

            var message = $"[{status}] {summary}（{description}）";
            var httpMessage = new HttpRequestMessage();
            httpMessage.Method = HttpMethod.Post;
            httpMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            httpMessage.Headers.Authorization = AuthenticationHeaderValue.Parse($"Bearer {gitterToken}");

            var gitterMessage = JsonSerializer.Serialize(new { text = message   });
            
            Console.WriteLine($"转发： {gitterMessage}");
            var content = new StringContent(gitterMessage);
            content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json");
            httpMessage.Content = content;
            httpMessage.RequestUri = new Uri(gitterRoomUrl);

            var response = await httpClient.SendAsync(httpMessage);
            var text = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"收到了来自 gitter 的响应：{response.StatusCode}");
            Console.WriteLine(text);
        }



        class AlertMessage
        {
            public string status { get; set; }
            public List<AlertMessageAlert> alerts { get; set; }
        }


        class AlertMessageAlert
        {
            public string status { get; set; }
            public Dictionary<string, string> labels { get; set; }
            public Dictionary<string, string> annotations { get; set; }
        }
    }
}
