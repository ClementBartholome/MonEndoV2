// using Quartz;
//
// namespace MonEndoVue.Server.Jobs;
//
// public class PingJob : IJob
// {
//     private readonly HttpClient _httpClient;
//     private readonly ILogger<PingJob> _logger;
//
//     public PingJob(IHttpClientFactory httpClientFactory, ILogger<PingJob> logger)
//     {
//         _httpClient = httpClientFactory.CreateClient("PingClient");
//         _httpClient.Timeout = TimeSpan.FromSeconds(30);
//         _logger = logger;
//     }
//
//     public async Task Execute(IJobExecutionContext context)
//     {
//         try
//         {
//             var response = await _httpClient.GetAsync("https://monendoapp.fr/", context.CancellationToken);
//             _logger.LogInformation("Ping status: {StatusCode} at {Time}", 
//                 response.StatusCode, DateTime.Now);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Failed to ping application");
//             throw;
//         }
//     }
// }
