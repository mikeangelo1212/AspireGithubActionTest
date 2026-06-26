
using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

// Add Docker Compose environment
var compose = builder.AddDockerComposeEnvironment("volumemount-env")
    .WithProperties(env =>
    {
        env.DashboardEnabled = true;
    })
    .ConfigureComposeFile(composeFile =>
     {
         // Add the blazor file volume to the top-level volumes section
         composeFile.AddVolume(new Volume
         {
            Name = "volumemount-blazor-uploads",
            Driver = "local"
         });
     });

var endpoint = builder.AddParameter("registry-endpoint");
var repository = builder.AddParameter("registry-repository");
#pragma warning disable ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddContainerRegistry("container-registry", endpoint, repository);
#pragma warning restore ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var cache = builder.AddRedis("cache")
            .PublishAsDockerComposeService((r, s) =>
            {
                
            });

var apiService = builder.AddProject<Projects.AspireCi_Demo_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .PublishAsDockerComposeService((r, s) =>
        {
            // NO PORTS
        });

builder.AddProject<Projects.AspireCi_Demo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Ports.Add("8080:80");

        service.AddVolume(new Volume
        {

            Name = "volumemount-blazor-uploads",
            Source = "volumemount-blazor-uploads",
            Target = "/app/uploads",
            Type = "volume"
        });


            //ONLY IF NEEDED
            // Override the entrypoint to allow write permissions to the volume
            // then run the default entrypoint as app user
            // service.User = "root";
            // service.Command = new List<string>
            // {
            //     "/bin/sh",
            //     "-c",
            //     "chown -R app:app /app/wwwroot/uploads && chmod -R 755 /app/wwwroot/uploads && exec su app -c 'dotnet /app/VolumeMount.BlazorWeb.dll'"
            // };

    });


builder.Build().Run();
