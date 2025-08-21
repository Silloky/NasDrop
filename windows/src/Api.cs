using RestSharp;
using RestSharp.Authenticators;
using Spectre.Console;
using System.Data.Common;
using System.Net;
using System.Text.Json.Serialization;

class Api
{
    public RestClient client;

    public Api(string? host = null)
    {
        client = new RestClient(new RestClientOptions(host ?? Program.config.ServerUrl)
        {
            Authenticator = Program.config.Auth.Token == "" ? null : new JwtAuthenticator(Program.config.Auth.Token)
        });
    }

    public class ApiRes<T>
    {
        public T Data { get; set; } = default!;
        public HttpStatusCode StatusCode { get; set; }
    }
    public class AuthRes
    {
        public string token { get; set; } = "";
        public DateTime expiry { get; set; } = DateTime.MinValue;
    }

    public class ShareCreationData
    {
        public string User { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
    }

    public class ShareAuthData
    {
        public string? Username { get; set; } = null;
        public string? Password { get; set; } = null;
    }

    public class Share
    {
        public string Id { get; set; } = "";
        public string Path { get; set; } = "";
        public ShareCreationData Creation { get; set; } = new ShareCreationData();
        public DateTime Expiry { get; set; } = DateTime.MinValue;
        public ShareAuthData Auth { get; set; } = new ShareAuthData();
        public int AccessCount { get; set; } = 0;
    }

    public class GetSharesRes : List<Share> { }

    public class ShareCreationReqBody
    {
        public string? WinPath { get; set; } = "";
        public int? Ttl { get; set; } = 0;
        public ShareAuthData? Auth { get; set; } = new ShareAuthData();
    }

    public bool canConnect()
    {
        var impatientClient = new RestClient(new RestClientOptions(Program.config.ServerUrl)
        {
            Timeout = new TimeSpan(0, 0, 0, 0, 400), // 400ms timeout
        });
        var pingResult = impatientClient.Execute<string>(new RestRequest("/api/ping", Method.Get));
        return pingResult.StatusCode == HttpStatusCode.OK;
    }

    public bool canConnectAfterWait(int? maxRetries = 6)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.BouncingBar)
            .SpinnerStyle(Style.Parse("blue"))
            .Start("Waiting for server connection...", ctx =>
            {
                int retryCount = 0;
                while (!canConnect() && retryCount < maxRetries)
                {
                    if (retryCount > 2)
                    {
                        ctx.Status("Retrying connection... Make sure you are connected to the network and/or VPN.");
                    }
                    Thread.Sleep(2000);
                    retryCount++;
                }

            });
        return canConnect() ? true : false;
    }

    public ApiRes<T> Request<T>(Method method, string path, object? body)
    {
        if (!canConnect())
        {
            Utils.WriteMessage("Cannot connect to the server. Please check your network and VPN connection", false);
            throw new Exception("Cannot connect to the server");
        }
        var request = new RestRequest(path, method);
        if (body != null)
        {
            request.AddJsonBody(body);
        }
        RestResponse<T> response = client.Execute<T>(request);

        return new ApiRes<T>
        {
            Data = response.Data,
            StatusCode = response.StatusCode
        };
    }
    
    

}