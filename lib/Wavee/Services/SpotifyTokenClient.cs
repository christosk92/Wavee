using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Eum.Spotify.clienttoken.data.v0;
using Eum.Spotify.clienttoken.http.v0;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using NeoSmart.AsyncLock;
using Wavee.Config;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Helpers;
using Wavee.Interfaces;
using Wavee.Models.Token;

namespace Wavee.Services;

internal sealed class SpotifyTokenClient : ISpotifyTokenClient
{
    private readonly ICredentialsProvider _credentialsProvider;
    private readonly AsyncLock _tokenLock = new();
    private SpotifyToken? _bearerToken;
    private SpotifyToken? _clientToken;

    private readonly SpotifyConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotifyTokenClient> _logger;

    public SpotifyTokenClient(
        ICredentialsProvider credentialsProvider,
        HttpClient httpClient,
        SpotifyConfig config,
        ILogger<SpotifyTokenClient> logger)
    {
        _credentialsProvider = credentialsProvider;
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public ValueTask<SpotifyToken> GetBearerToken(CancellationToken cancellationToken = default)
    {
        if (_bearerToken is not null && !_bearerToken.Value.IsExpired)
        {
            return new ValueTask<SpotifyToken>(_bearerToken.Value);
        }

        return new ValueTask<SpotifyToken>(FetchBearerAsync(cancellationToken));
    }

    public ValueTask<SpotifyToken> GetClientToken(CancellationToken cancellationToken = default)
    {
        if (_clientToken is not null && !_clientToken.Value.IsExpired)
        {
            return new ValueTask<SpotifyToken>(_clientToken.Value);
        }

        return new ValueTask<SpotifyToken>(FetchClientAsync(cancellationToken));
    }

    private async Task<SpotifyToken> FetchClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching client token because it is either null or expired");
            using (await _tokenLock.LockAsync(cancellationToken))
            {
                const string endpoint = "https://clienttoken.spotify.com/v1/clienttoken";
                var clientTokenRequest = new ClientTokenRequest
                {
                    ClientData = new ClientDataRequest
                    {
                        ClientVersion = "1.2.31.1205.g4d59ad7c",
                        ClientId = _credentialsProvider.ClientId,
                        ConnectivitySdkData = new ConnectivitySdkData
                        {
                            DeviceId = _config.Playback.DeviceId,
                            PlatformSpecificData = FillPlatformData()
                        }
                    }
                };

                int count = 0;
                const int MAX_RETRIES = 3;
                while (true)
                {
                    count++;
                    var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    {
                        Content = new ByteArrayContent(clientTokenRequest.ToByteArray())
                    };
                    //accept x-protobuf
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                    _logger.LogDebug("Sending client token request to Spotify");


                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    _logger.LogDebug("Received response from Spotify");
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var clientTokenResponse = ClientTokenResponse.Parser.ParseFrom(stream);
                    switch (clientTokenResponse.ResponseType)
                    {
                        case ClientTokenResponseType.ResponseGrantedTokenResponse:
                            _logger.LogInformation("Received new client token");
                            var expiresAt =
                                DateTimeOffset.UtcNow.AddSeconds(clientTokenResponse.GrantedToken.ExpiresAfterSeconds);
                            var token = new SpotifyToken(Value: clientTokenResponse.GrantedToken.Token,
                                SpotifyTokenType.ClientToken, expiresAt);
                            _clientToken = token;
                            return token;
                        case ClientTokenResponseType.ResponseChallengesResponse:
                            _logger.LogWarning("Received a hash cash challenge, solving...");
                            var challenges = clientTokenResponse.Challenges.Challenges;
                            var state = clientTokenResponse.Challenges.State;
                            var challenge = challenges.FirstOrDefault();
                            if (challenge is not null)
                            {
                                var hashCashChallenge = challenge.EvaluateHashcashParameters;
                                var length = hashCashChallenge.Length;

                                // Decode the prefix from hex
                                byte[] prefix;
                                try
                                {
                                    prefix = HexStringToByteArray(hashCashChallenge.Prefix);
                                }
                                catch (Exception ex)
                                {
                                    throw new WaveeCouldNotAuthenticateException(
                                        $"Unable to decode hash cash challenge: {ex.Message}");
                                }

                                var ctx = Array.Empty<byte>(); // Empty context as in Rust code
                                byte[] suffix;
                                TimeSpan duration;
                                var solved =
                                    HashCashSolver.SolveHashCash(ctx, prefix, length, out suffix, out duration);
                                if (solved)
                                {
                                    _logger.LogInformation("Solved hash cash challenge in {duration}", duration);
                                    var suffixHex = ByteArrayToHexString(suffix).ToUpperInvariant();
                                    var answerMessage = new ClientTokenRequest
                                    {
                                        RequestType = ClientTokenRequestType.RequestChallengeAnswersRequest,
                                        ChallengeAnswers = new ChallengeAnswersRequest()
                                        {
                                            State = state
                                        },
                                    };

                                    var challengeAnswer = new ChallengeAnswer
                                    {
                                        ChallengeType = ChallengeType.ChallengeHashCash,
                                        HashCash = new HashCashAnswer
                                        {
                                            Suffix = suffixHex
                                        }
                                    };
                                    answerMessage.ChallengeAnswers.Answers.Add(challengeAnswer);
                                    clientTokenRequest = answerMessage;
                                    _logger.LogTrace("Answering hash cash challenge");
                                    continue;
                                }
                            }

                            break;
                        case ClientTokenResponseType.ResponseUnknown:
                            _logger.LogError("Received an unknown response from Spotify");
                            throw new WaveeUnknownException("Received an unknown response from Spotify");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (count > MAX_RETRIES)
                    {
                        _logger.LogError("Failed to fetch client token after {MAX_RETRIES} retries", MAX_RETRIES);
                        throw new WaveeUnknownException($"Failed to fetch client token after {MAX_RETRIES} retries");
                    }
                }
            }

            return default;
        }
        catch (Exception ex) when (ex is not WaveeException)
        {
            _logger.LogError(ex, "Failed to fetch client token");
            throw new WaveeUnknownException("An unknown error occurred while fetching the client token", ex);
        }

        static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Invalid hex string length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }

    private async Task<SpotifyToken> FetchBearerAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching bearer token because it is either null or expired");
            using (await _tokenLock.LockAsync(cancellationToken))
            {
                var credentials = await _credentialsProvider.GetUserCredentialsAsync(cancellationToken);
                const string endpoint = "https://login5.spotify.com/v3/login";
                var loginRequest = new LoginRequest
                {
                    ClientInfo = new ClientInfo
                    {
                        ClientId = _credentialsProvider.ClientId,
                        DeviceId = _config.Playback.DeviceId
                    },
                    StoredCredential = new StoredCredential
                    {
                        Data = credentials.AuthData,
                        Username = credentials.Username
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new ByteArrayContent(loginRequest.ToByteArray())
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                _logger.LogDebug("Sending login request to Spotify");
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                _logger.LogDebug("Received response from Spotify");
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var loginResponse = LoginResponse.Parser.ParseFrom(stream);
                var expiresAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(loginResponse.Ok.AccessTokenExpiresIn));
                var token = new SpotifyToken(Value: loginResponse.Ok.AccessToken, SpotifyTokenType.Bearer, expiresAt);
                _bearerToken = token;
                _logger.LogInformation("Received new bearer token");
                return token;
            }
        }
        catch (Exception ex) when (ex is not WaveeException)
        {
            _logger.LogError(ex, "Failed to fetch bearer token");
            throw new WaveeUnknownException("An unknown error occurred while fetching the bearer token", ex);
        }
    }

    private PlatformSpecificData FillPlatformData()
    {
        var platformData = new PlatformSpecificData();
        var osDescription = RuntimeInformation.OSDescription;
        var osPlatform = GetOSPlatform();
        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

        if (osPlatform == OSPlatform.Windows)
        {
            var windowsData = new NativeDesktopWindowsData();

            // Get OS Version
            var osVersion = GetWindowsOSVersion();

            // Get Kernel Version
            var kernelVersion = GetWindowsKernelVersion();

            // Map architecture to pe_machine and image_file_machine
            var (peMachine, imageFileMachine) = GetWindowsArchitectureInfo(architecture);

            windowsData.OsVersion = osVersion;
            windowsData.OsBuild = kernelVersion;
            windowsData.PlatformId = 2; // Corresponds to PlatformID.Win32NT
            windowsData.UnknownValue6 = 9;
            windowsData.ImageFileMachine = imageFileMachine;
            windowsData.PeMachine = peMachine;
            windowsData.UnknownValue10 = true;

            platformData.DesktopWindows = windowsData;
        }
        else if (osPlatform == OSPlatform.OSX)
        {
            var macData = new NativeDesktopMacOSData();

            var systemVersion = GetMacOSVersion();
            macData.SystemVersion = systemVersion;
            macData.HwModel = "iMac21,1";
            macData.CompiledCpuType = architecture;

            platformData.DesktopMacos = macData;
        }
        else if (osPlatform == OSPlatform.Linux)
        {
            var linuxData = new NativeDesktopLinuxData();

            linuxData.SystemName = "Linux";
            linuxData.SystemRelease = GetLinuxKernelVersion();
            linuxData.SystemVersion = GetLinuxOSVersion();
            linuxData.Hardware = architecture;

            platformData.DesktopLinux = linuxData;
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }

        return platformData;
    }

    private int GetWindowsOSVersion()
    {
        // Use Environment.OSVersion.Version to get the OS version
        var version = Environment.OSVersion.Version;
        // For example, Windows 10 is version 10.x
        return version.Major;
    }

    private int GetWindowsKernelVersion()
    {
        // Kernel version might not be directly available.
        // As an approximation, use the Build number
        var version = Environment.OSVersion.Version;
        return version.Build;
    }

    private (int peMachine, int imageFileMachine) GetWindowsArchitectureInfo(string architecture)
    {
        return architecture switch
        {
            "arm" => (448, 452),
            "arm64" => (43620, 452),
            "x64" => (34404, 34404),
            _ => (332, 332) // x86
        };
    }

    private string GetMacOSVersion()
    {
        // Use sw_vers command to get macOS version
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/sw_vers",
                    Arguments = "-productVersion",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return result;
        }
        catch
        {
            return "11.0"; // Default to Big Sur
        }
    }

    private string GetLinuxKernelVersion()
    {
        // Read from /proc/version or uname -r
        try
        {
            var kernelVersion = System.IO.File.ReadAllText("/proc/version");
            return kernelVersion;
        }
        catch
        {
            return "0";
        }
    }

    private string GetLinuxOSVersion()
    {
        // Read from /etc/os-release or use uname
        try
        {
            var osRelease = System.IO.File.ReadAllText("/etc/os-release");
            // Parse the VERSION_ID field
            var lines = osRelease.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("VERSION_ID="))
                {
                    var version = line.Substring("VERSION_ID=".Length).Trim('\"');
                    return version;
                }
            }

            return "0";
        }
        catch
        {
            return "0";
        }
    }

    private OSPlatform GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OSPlatform.Linux;
        }

        return OSPlatform.FreeBSD; // Default to FreeBSD if none match
    }
}