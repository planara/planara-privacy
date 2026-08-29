using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Planara.Common.Kafka.Messages.Privacy;
using Planara.Kafka.Interfaces;
using Planara.Privacy.Data;
using Planara.Privacy.Workers;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Planara.Privacy.Tests;

public class ApiTestWebAppFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:latest")
        .WithDatabase("privacy-test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:latest").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<DataContext>));
            services.RemoveAll(typeof(DataContext));
            services.RemoveAll(typeof(DbContext));
            services.RemoveAll(typeof(IConnectionMultiplexer));

            services.RemoveAll<IKafkaProducer<ConsentGrantedMessage>>();
            services.RemoveAll<IKafkaProducer<ConsentRevokedMessage>>();
            services.RemoveAll<IKafkaConsumer<ConsentGrantRequestedMessage>>();

            services.RemoveAll<IHostedService>();

            services.AddSingleton<FakeKafkaProducer<ConsentGrantedMessage>>();
            services.AddSingleton<FakeKafkaProducer<ConsentRevokedMessage>>();

            services.AddSingleton<IKafkaProducer<ConsentGrantedMessage>>(sp => 
                sp.GetRequiredService<FakeKafkaProducer<ConsentGrantedMessage>>());

            services.AddSingleton<IKafkaProducer<ConsentRevokedMessage>>(sp =>
                sp.GetRequiredService<FakeKafkaProducer<ConsentRevokedMessage>>());

            services.AddSingleton<FakeKafkaConsumer<ConsentGrantRequestedMessage>>();

            services.AddSingleton<IKafkaConsumer<ConsentGrantRequestedMessage>>(sp =>
                sp.GetRequiredService<FakeKafkaConsumer<ConsentGrantRequestedMessage>>());

            services.AddScoped<ConsentGrantedOutboxPublisher>();
            services.AddScoped<ConsentRevokedOutboxPublisher>();
            services.AddScoped<ConsentGrantRequestedKafkaConsumerWorker>();

            services.AddDbContext<DataContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));

            services.AddScoped<DbContext>(sp => sp.GetRequiredService<DataContext>());

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            });
        });

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("DbConnections:Redis:ConnectionString", _redis.GetConnectionString()!),

                new KeyValuePair<string, string>("GraphQL:Name", "test-privacy-schema")
            }!);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        db.Database.SetCommandTimeout(3000);

        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await _redis.StopAsync();
    }
}