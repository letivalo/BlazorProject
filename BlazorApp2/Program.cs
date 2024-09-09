using BlazorApp2.Components;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.OpenApi;
using System;
using BlazorApp2.Client;


namespace BlazorApp2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddHttpClient();
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseAddress"]) });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();

            // Middleware configuration, e.g., Swagger, static files, etc.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            // Set connection string equal to the one defined in appsettings.json to authenticate before proceeding
            string connectionString = app.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")!;

            // Open Connection to SQL server based on connection string.
            using var conn = new SqlConnection(connectionString);
                conn.Open();

                // If table Todos does not already exist, create the table.
                var checkTableCmd = new SqlCommand(
                    // ID value is set to an IDENTITY column by SQL here, meaning each value should automatically be assigned an ID.
                    "IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Todos') CREATE TABLE Todos (ID int NOT NULL PRIMARY KEY IDENTITY, Title varchar(255), IsDone varchar(255));",
                    conn);
                checkTableCmd.ExecuteNonQuery();

            // SQL GET ENDPOINT
            app.MapGet("/Todos", () => {
                var todos = new List<Todos>();

                using var conn = new SqlConnection(connectionString);
                conn.Open();

                var command = new SqlCommand("SELECT ID, Title, IsDone FROM Todos", conn);
                using SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        todos.Add(new Todos
                        {
                            ID = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            IsDone = reader.GetString(2) == "1" // Assuming "1" means true and "0" means false
                        });
                    }
                }
                return todos;
            })
            .WithName("GetTodos")
            .WithOpenApi();

            // SQL POST ENDPOINT
            app.MapPost("/Todos", (Todos newTask) =>
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                var command = new SqlCommand(
                    "INSERT INTO Todos (Title, IsDone) VALUES (@Title, @IsDone)",
                    conn);

                command.Parameters.Clear();
                command.Parameters.AddWithValue("@Title", newTask.Title);
                command.Parameters.AddWithValue("@Isdone", newTask.IsDone);

                command.ExecuteNonQuery();
            })
            .WithName("CreateTodos")
            .WithOpenApi();

            // SQL PUT ENDPOINT
            app.MapPut("/Todos/{id}", (int id, Todos updatedTodo) => {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                var command = new SqlCommand(
                    "UPDATE Todos SET Title = @Title, IsDone = @IsDone WHERE ID = @ID",
                    conn);

                command.Parameters.AddWithValue("@Title", updatedTodo.Title);
                command.Parameters.AddWithValue("@IsDone", updatedTodo.IsDone ? "1" : "0");
                command.Parameters.AddWithValue("@ID", id);

                command.ExecuteNonQuery();
            })
            .WithName("UpdateTodo")
            .WithOpenApi();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
