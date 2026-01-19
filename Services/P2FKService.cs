using NBitcoin;
using SupStick.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SupStick.Services
{
    /// <summary>
    /// P2FK message parsing service - adapted from Root.cs
    /// </summary>
    public class P2FKService : IP2FKService
    {
        private readonly IBitcoinService _bitcoinService;
        private readonly char[] _specialChars = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        private readonly Regex _regexSpecialChars = new Regex(@"([\\/:*?""<>|])\d+");

        // P2FK known microtransaction values on testnet
        private readonly HashSet<string> _allowedValues = new HashSet<string>
        {
            "0.00000001", "0.00000546", "0.00000548", "0.00005480",
            "0.00000550", "0.00005500", "0.00001000", "0.01000000",
            "0.02000000", "1"
        };

        public P2FKService(IBitcoinService bitcoinService)
        {
            _bitcoinService = bitcoinService;
        }

        public async Task<Dictionary<string, string>> GetTransactionOutputsAsync(string transactionId)
        {
            var outputs = new Dictionary<string, string>();

            try
            {
                var txDetails = await _bitcoinService.GetTransactionDetailsAsync(transactionId);
                if (txDetails == null || !txDetails.ContainsKey("vout"))
                    return outputs;

                var voutJson = txDetails["vout"].ToString();
                var vouts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(voutJson ?? "[]");

                if (vouts != null)
                {
                    foreach (var vout in vouts)
                    {
                        if (vout.ContainsKey("scriptPubKey") && vout.ContainsKey("value"))
                        {
                            var scriptPubKey = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                vout["scriptPubKey"].ToString() ?? "{}");

                            if (scriptPubKey != null && scriptPubKey.ContainsKey("addresses"))
                            {
                                var addresses = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(
                                    scriptPubKey["addresses"].ToString() ?? "[]");

                                if (addresses != null && addresses.Count > 0)
                                {
                                    outputs[addresses[0]] = vout["value"].ToString() ?? "0";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting outputs: {ex.Message}");
            }

            return outputs;
        }

        public async Task<P2FKRoot?> ParseTransactionAsync(string transactionId)
        {
            try
            {
                var txDetails = await _bitcoinService.GetTransactionDetailsAsync(transactionId);
                if (txDetails == null)
                    return null;

                var outputs = await GetTransactionOutputsAsync(transactionId);
                var transactionBytes = await ExtractP2FKBytesFromOutputsAsync(outputs);

                if (transactionBytes == null || transactionBytes.Length == 0)
                    return null;

                var root = ParseFromTransactionData(transactionBytes, transactionId);

                if (root != null)
                {
                    root.Output = outputs;

                    // Extract block information if available
                    if (txDetails.ContainsKey("confirmations"))
                    {
                        root.Confirmations = Convert.ToInt32(txDetails["confirmations"]);
                    }

                    if (txDetails.ContainsKey("blocktime"))
                    {
                        var blocktime = Convert.ToInt64(txDetails["blocktime"]);
                        root.BlockDate = DateTimeOffset.FromUnixTimeSeconds(blocktime).DateTime;
                    }

                    if (txDetails.ContainsKey("size"))
                    {
                        root.TotalByteSize = Convert.ToInt32(txDetails["size"]);
                    }
                }

                return root;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing transaction {transactionId}: {ex.Message}");
                return null;
            }
        }

        private async Task<byte[]?> ExtractP2FKBytesFromOutputsAsync(Dictionary<string, string> outputs)
        {
            var transactionBytes = Array.Empty<byte>();

            try
            {
                foreach (var output in outputs)
                {
                    var address = output.Key;
                    var value = output.Value;

                    // Check if this is a P2FK micro-transaction
                    if (_allowedValues.Contains(value))
                    {
                        try
                        {
                            // Decode the address to extract payload
                            var decoded = DecodeBase58WithChecksum(address);
                            if (decoded != null && decoded.Length > 1)
                            {
                                // Skip the version byte (first byte)
                                var payload = decoded.Skip(1).ToArray();

                                // Append to transaction bytes
                                var combined = new byte[transactionBytes.Length + payload.Length];
                                Buffer.BlockCopy(transactionBytes, 0, combined, 0, transactionBytes.Length);
                                Buffer.BlockCopy(payload, 0, combined, transactionBytes.Length, payload.Length);
                                transactionBytes = combined;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error decoding address {address}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting P2FK bytes: {ex.Message}");
            }

            return transactionBytes.Length > 0 ? transactionBytes : null;
        }

        public P2FKRoot? ParseFromTransactionData(byte[] transactionBytes, string transactionId)
        {
            var root = new P2FKRoot
            {
                TransactionId = transactionId,
                BuildDate = DateTime.UtcNow
            };

            try
            {
                var files = new Dictionary<string, BigInteger>();
                var keywords = new Dictionary<string, string>();
                var messageList = new List<string>();

                int transactionBytesSize = transactionBytes.Length;
                string transactionASCII = Encoding.ASCII.GetString(transactionBytes);

                // Parse P2FK formatted data
                while (_regexSpecialChars.IsMatch(transactionASCII))
                {
                    var match = _regexSpecialChars.Match(transactionASCII);
                    int packetSize;
                    int headerSize;

                    try
                    {
                        packetSize = int.Parse(match.Value.Substring(1), NumberStyles.Any, CultureInfo.InvariantCulture);
                        headerSize = match.Index + match.Length + 1;
                    }
                    catch
                    {
                        break;
                    }

                    // Invalid if a special character is not found before a number
                    if (transactionASCII.IndexOfAny(_specialChars) != match.Index)
                    {
                        break;
                    }

                    string fileName = transactionASCII.Substring(0, match.Index);
                    byte[] fileBytes = transactionBytes
                        .Skip(headerSize + (transactionBytesSize - transactionASCII.Length))
                        .Take(packetSize)
                        .ToArray();

                    bool isValid = IsValidFileName(fileName);

                    if (isValid)
                    {
                        files[fileName] = fileBytes.Length;
                    }
                    else
                    {
                        // Check if this is a message (empty filename with content)
                        if (fileName == "" && fileBytes.Length > 1)
                        {
                            messageList.Add(Encoding.UTF8.GetString(fileBytes));
                        }
                        else
                        {
                            break;
                        }
                    }

                    try
                    {
                        // Remove processed header and payload bytes
                        transactionASCII = transactionASCII.Substring(packetSize + headerSize);
                    }
                    catch
                    {
                        transactionASCII = "";
                        break;
                    }
                }

                // If no files or messages were found, return null
                if (files.Count == 0 && messageList.Count == 0)
                {
                    return null;
                }

                root.File = files;
                root.Message = messageList.ToArray();
                root.Keyword = keywords;

                return root;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing P2FK data: {ex.Message}");
                return null;
            }
        }

        private bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            // Check for valid file names (with extension or known types)
            if (fileName.Length > 2 && fileName.Contains("."))
                return true;

            // Known file types without extension
            if (fileName.Length == 3 && "BTC,LTC,DOG,MZC,IPFS".Contains(fileName))
                return false;

            // Transaction ID (64 hex characters)
            if (!fileName.Contains(".") && fileName.Length == 64)
                return false;

            return false;
        }

        private byte[]? DecodeBase58WithChecksum(string address)
        {
            try
            {
                var decoded = NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(address);
                return decoded;
            }
            catch
            {
                return null;
            }
        }
    }
}
