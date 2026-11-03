using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<MassiveLoadWorker>();

var host = builder.Build();
host.Run();
