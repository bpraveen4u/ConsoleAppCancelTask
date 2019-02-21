using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppCancelTask
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting application...");

            CancellationTokenSource source = new CancellationTokenSource();
            //assuming the wrapping class is TplTest
            var task = new MyClass().CancellableTask(source.Token);

            Console.WriteLine("Heavy process invoked");

            Console.WriteLine("Press C to cancel");
            Console.WriteLine("");
            char ch = Console.ReadKey().KeyChar;
            if (ch == 'c' || ch == 'C')
            {
                source.Cancel();
                Console.WriteLine("\nTask cancellation requested.");
            }

            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Any(e => e is TaskCanceledException))
                    Console.WriteLine("Task cancelled exception detected");
                else
                {
                    Console.WriteLine(ae);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
            finally
            {
                source.Dispose();
            }

            Console.WriteLine("Process completed");
            Console.ReadKey();
        }
    }

    public class MyClass
    {
        private async Task CancellableWork(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancelled work before start");
                cancellationToken.ThrowIfCancellationRequested();
            }

            for (int i = 0; i < 10; i++)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-f-key", "121");

                    var input = new { batchId = i };
                    HttpContent content = new StringContent(JsonConvert.SerializeObject(input), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://postb.in/0Fd8i8Du", content, cancellationToken);
                    if (response != null && !response.IsSuccessStatusCode)
                    {
                        string error = response.Content.ToString();
                    }

                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"Cancelled on iteration # {i + 1}");
                    //the following lien alone is enough to check and throw
                    cancellationToken.ThrowIfCancellationRequested();
                }
                Console.WriteLine($"Iteration # {i + 1} completed");
            }
        }

        public Task CancellableTask(CancellationToken ct)
        {
            return Task.Factory.StartNew(() => CancellableWork(ct), ct);
        }

    }
}
