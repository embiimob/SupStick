using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// IPFS service implementation with retry mechanism
    /// </summary>
    public class IpfsService : IIpfsService
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _gateways = new[]
        {
            "https://ipfs.io/ipfs/",
            "https://gateway.pinata.cloud/ipfs/",
            "https://cloudflare-ipfs.com/ipfs/",
            "https://dweb.link/ipfs/"
        };

        public IpfsService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public string GetGatewayUrl(string ipfsHash)
        {
            // Use the first gateway by default
            return $"{_gateways[0]}{ipfsHash}";
        }

        public string? ExtractIpfsHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            // Match pattern like <<IPFS:QmSdw1n...gedUg\sup.wav>>
            var ipfsPattern = @"<<IPFS:([a-zA-Z0-9]+)(?:\\[^>]+)?>>";
            var match = Regex.Match(content, ipfsPattern);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            // Also check for standalone IPFS hashes (Qm... format)
            var hashPattern = @"\b(Qm[a-zA-Z0-9]{44,})\b";
            match = Regex.Match(content, hashPattern);

            if (match.Success)
            {
                return match.Value;
            }

            return null;
        }

        public async Task<byte[]?> DownloadFileAsync(string ipfsHash)
        {
            try
            {
                var url = GetGatewayUrl(ipfsHash);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                Console.WriteLine($"Failed to download from IPFS: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading from IPFS: {ex.Message}");
            }

            return null;
        }

        public async Task<byte[]?> DownloadFileWithRetryAsync(string ipfsHash, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // Try different gateways on subsequent retries
                var gatewayIndex = attempt % _gateways.Length;
                var url = $"{_gateways[gatewayIndex]}{ipfsHash}";

                try
                {
                    Console.WriteLine($"Attempting to download from IPFS (attempt {attempt + 1}/{maxRetries}): {url}");

                    var response = await _httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsByteArrayAsync();
                        Console.WriteLine($"Successfully downloaded {data.Length} bytes from IPFS");
                        return data;
                    }

                    Console.WriteLine($"Failed to download from IPFS gateway {gatewayIndex}: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading from IPFS gateway {gatewayIndex}: {ex.Message}");
                }

                // Wait before retrying (exponential backoff)
                if (attempt < maxRetries - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    Console.WriteLine($"Waiting {delay.TotalSeconds} seconds before retry...");
                    await Task.Delay(delay);
                }
            }

            Console.WriteLine($"Failed to download from IPFS after {maxRetries} attempts");
            return null;
        }
    }
}
