using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

internal class Program
{
    static HttpClient _http0 = new HttpClient();
    static HttpClient _http1 = new HttpClient();
    static HttpClient _http2 = new HttpClient();
    static HttpClient _http3 = new HttpClient();
    static HttpClient _http4 = new HttpClient();
    static HttpClient _http5 = new HttpClient();

    private static async Task Main(string[] args)
    {
        string url = "https://messagingexamples.azurewebsites.net/api/ServiceBusBatchBindingSenderHttp";
        
        Console.WriteLine("Starting 6 runners after interval of 60 seconds, 10 seconds apart");


        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(0)).Subscribe(async a => {
            Console.WriteLine($"HTTP00:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http0.PostAsync(url, null);
            Console.WriteLine($"HTTP00:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(10)).Subscribe(async a => {
            Console.WriteLine($"HTTP10:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http1.PostAsync(url, null);
            Console.WriteLine($"HTTP10:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(20)).Subscribe(async a => {
            Console.WriteLine($"HTTP20:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http2.PostAsync(url, null);
            Console.WriteLine($"HTTP20:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(30)).Subscribe(async a => {
            Console.WriteLine($"HTTP30:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http3.PostAsync(url, null);
            Console.WriteLine($"HTTP30:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(40)).Subscribe(async a => {
            Console.WriteLine($"HTTP40:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http4.PostAsync(url, null);
            Console.WriteLine($"HTTP40:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        Observable.Interval(TimeSpan.FromSeconds(60)).Delay(TimeSpan.FromSeconds(50)).Subscribe(async a => {
            Console.WriteLine($"HTTP50:{a}: {DateTime.Now:O} POST {url}");
            var response = await _http5.PostAsync(url, null);
            Console.WriteLine($"HTTP50:{a}: {DateTime.Now:O} {(int)response.StatusCode} {response.ReasonPhrase}");
        });

        while (!Console.KeyAvailable)
        {
            await Task.Delay(1);
        }

    }
}