using BlazorApp2.Components; // Import components for the Blazor app
using Microsoft.Data.SqlClient; // SQL Client to interact with SQL Server
using Microsoft.AspNetCore.OpenApi; // For OpenAPI (Swagger) support
using System; // Import system base libraries
using BlazorApp2.Client;
using System.Data; // Import the Blazor client components

namespace BlazorApp2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the dependency injection container

            // Add HttpClient service to allow HTTP requests
            builder.Services.AddHttpClient();

            // Configure HttpClient with a base address from configuration (e.g., appsettings.json)
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseAddress"]) });

            // Add support for API documentation and exploration via Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Register Razor components and enable both server and web assembly modes for interactivity
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            // Register SQL connection string using dependency injection (gets value from appsettings.json)
            var connectionString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            builder.Services.AddScoped(_ => new SqlConnection(connectionString)); // Register SqlConnection with DI

            // Build the application pipeline
            var app = builder.Build();

            // Call middleware configuration method (defined below)
            ConfigureMiddleware(app);

            // Map GET endpoint to fetch todos from the database
            app.MapGet("/Todos", async (SqlConnection conn) =>
            {
                // List to store fetched to-do items
                var todos = new List<UserEntryModel>();

                // Open a connection to the SQL database
                await conn.OpenAsync();

                // SQL command to fetch ID, Title, and IsDone status from Todos table
                using var command = new SqlCommand("SELECT ID, Title, IsDone FROM Todos", conn);

                // Execute the SQL command and read the results
                using SqlDataReader reader = await command.ExecuteReaderAsync();

                // If there are rows, read each row and add to the todo list
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        todos.Add(new UserEntryModel
                        {
                            ID = reader.GetInt32(0),        // Get ID (int)
                            Title = reader.GetString(1),    // Get Title (string)
                            IsDone = reader.GetBoolean(2)   // Get IsDone status (boolean from SQL 'bit')
                        });
                    }
                }
                return todos; // Return the list of todos
            })
            .WithName("GetTodos") // Name the API route for clarity
            .WithOpenApi(); // Enable OpenAPI documentation

            // Map POST endpoint to add a new todo item
            app.MapPost("/Todos", async (UserEntryModel UserInput, SqlConnection conn) =>
            {
                // Open the SQL connection
                await conn.OpenAsync();

                // SQL command to insert a new to-do item with Title and IsDone status
                using var command = new SqlCommand(
                    "INSERT INTO Todos (Title, IsDone) VALUES (@Title, @IsDone)", conn);

                // Why is this a duplication of the need to create UserInputSubmission in Demo.razor?
                command.Parameters.AddWithValue("@Title", UserInput.Title); // Bind title
                command.Parameters.AddWithValue("@IsDone", UserInput.IsDone ? 1 : 0); // Convert boolean to 1/0 for SQL

                // Execute the insert command
                await command.ExecuteNonQueryAsync();
                
            })
            .WithName("CreateTodos") // Name the API route
            .WithOpenApi(); // Enable OpenAPI documentation
            

            // Map PUT endpoint to update an existing todo item by its ID
            app.MapPut("/Todos/{id}", async (int id, UserEntryModel updatedTodo, SqlConnection conn) =>
            {
                // Open the SQL connection
                await conn.OpenAsync();

                // SQL command to update the title and IsDone status of the specified todo item
                using var command = new SqlCommand(
                    "UPDATE Todos SET Title = @Title, IsDone = @IsDone WHERE ID = @ID", conn);

                // Add parameters for Title, IsDone, and ID
                command.Parameters.AddWithValue("@Title", updatedTodo.Title); // Bind title
                command.Parameters.AddWithValue("@IsDone", updatedTodo.IsDone ? 1 : 0); // Bind IsDone status as 1/0
                command.Parameters.AddWithValue("@ID", id); // Bind ID to identify the row to update

                // Execute the update command
                await command.ExecuteNonQueryAsync();
            })
            .WithName("UpdateTodo") // Name the API route
            .WithOpenApi(); // Enable OpenAPI documentation

            // Map the Razor components (UI) and enable both server-side and web assembly rendering modes
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode() // Add interactive server rendering
                .AddInteractiveWebAssemblyRenderMode() // Add interactive WebAssembly rendering
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly); // Include additional assemblies for components

            // Run the web application (start the server and handle incoming requests)
            app.Run();
        }

        // Method to configure middleware, which processes HTTP requests
        private static void ConfigureMiddleware(WebApplication app)
        {
            // Middleware for development environment
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging(); // Enable WebAssembly debugging for development
                app.UseSwagger(); // Enable Swagger for API documentation
                app.UseSwaggerUI(); // Enable Swagger UI for exploring API documentation
            }
            else
            {
                app.UseExceptionHandler("/Error"); // Use global error handler for production
                app.UseHsts(); // Enforce strict HTTPS for production
            }

            // Middleware to handle HTTP requests
            app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
            app.UseStaticFiles(); // Serve static files (like images, CSS, etc.)
            app.UseAntiforgery(); // Use anti-forgery tokens for form submissions
        }
    }
}
