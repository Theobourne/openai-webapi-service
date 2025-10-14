using System.Text;
using System.Xml;

namespace SoapClient
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string SoapServiceUrl = "http://localhost:5166/soap/AiTextService.asmx";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== SOAP/XML Web Service Client for AI Text Generation ===");
            Console.WriteLine($"Connecting to SOAP service at {SoapServiceUrl}");
            
            try
            {
                // Step 1: Submit a request via SOAP
                Console.WriteLine("\n1. Submitting text generation request via SOAP...");
                string requestId = await SubmitTextRequest("Write a limerick about XML and SOAP protocols");
                
                if (!string.IsNullOrEmpty(requestId))
                {
                    Console.WriteLine($"✓ Request submitted successfully!");
                    Console.WriteLine($"  Request ID: {requestId}");
                    
                    // Step 2: Poll for status until completion
                    Console.WriteLine($"\n2. Polling for status via SOAP...");
                    await PollForCompletion(requestId);
                }
                else
                {
                    Console.WriteLine("✗ Failed to submit request");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error occurred: {ex.Message}");
                Console.WriteLine("Make sure the server is running on http://localhost:5166");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static async Task<string> SubmitTextRequest(string prompt)
        {
            var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <SubmitTextRequest xmlns=""http://webapi.distributedlab.com/soap"">
      <request>
        <Prompt>{prompt}</Prompt>
        <PreviousRequestId></PreviousRequestId>
      </request>
    </SubmitTextRequest>
  </soap:Body>
</soap:Envelope>";

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://webapi.distributedlab.com/soap/IAiTextSoapService/SubmitTextRequest");

            var response = await httpClient.PostAsync(SoapServiceUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"  SOAP Response Status: {response.StatusCode}");
            
            // Parse the SOAP response to extract the request ID
            var requestId = ExtractRequestIdFromResponse(responseContent);
            return requestId;
        }

        static async Task PollForCompletion(string requestId)
        {
            int pollCount = 0;
            
            while (true)
            {
                pollCount++;
                
                var soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetRequestStatus xmlns=""http://webapi.distributedlab.com/soap"">
      <request>
        <RequestId>{requestId}</RequestId>
      </request>
    </GetRequestStatus>
  </soap:Body>
</soap:Envelope>";

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Add("SOAPAction", "http://webapi.distributedlab.com/soap/IAiTextSoapService/GetRequestStatus");

                var response = await httpClient.PostAsync(SoapServiceUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var (status, result) = ExtractStatusFromResponse(responseContent);
                
                Console.WriteLine($"  Poll #{pollCount} - Status: {status}");
                
                if (status == "COMPLETE")
                {
                    Console.WriteLine($"\n✓ Request completed successfully!");
                    Console.WriteLine($"  Generated Text: {result}");
                    break;
                }
                else if (status == "FAILED")
                {
                    Console.WriteLine($"\n✗ Request failed!");
                    Console.WriteLine($"  Error: {result}");
                    break;
                }
                
                // Wait before polling again
                if (status == "PENDING" || status == "PROCESSING")
                {
                    Console.WriteLine("    Waiting 5 seconds before next poll...");
                    await Task.Delay(5000);
                }
            }
        }

        static string ExtractRequestIdFromResponse(string soapResponse)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(soapResponse);
                
                var namespaceManager = new XmlNamespaceManager(doc.NameTable);
                namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                namespaceManager.AddNamespace("ns", "http://webapi.distributedlab.com/soap");
                
                var requestIdNode = doc.SelectSingleNode("//ns:RequestId", namespaceManager);
                return requestIdNode?.InnerText ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SOAP response: {ex.Message}");
                Console.WriteLine($"Response content: {soapResponse}");
                return "";
            }
        }

        static (string status, string result) ExtractStatusFromResponse(string soapResponse)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(soapResponse);
                
                var namespaceManager = new XmlNamespaceManager(doc.NameTable);
                namespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                namespaceManager.AddNamespace("ns", "http://webapi.distributedlab.com/soap");
                
                var statusNode = doc.SelectSingleNode("//ns:Status", namespaceManager);
                var resultNode = doc.SelectSingleNode("//ns:Result", namespaceManager);
                
                return (statusNode?.InnerText ?? "UNKNOWN", resultNode?.InnerText ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SOAP response: {ex.Message}");
                return ("ERROR", $"Parse error: {ex.Message}");
            }
        }
    }
}