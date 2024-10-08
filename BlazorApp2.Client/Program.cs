using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorApp2.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            await builder.Build().RunAsync();
        }
    }

    public class UserEntryModel
    {
        public int ID { get; set; }  // Make sure this property exists
        public string? Title { get; set; }
        public bool IsDone { get; set; } = false;
    }
}
