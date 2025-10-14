using System.Runtime.Serialization;
using System.ServiceModel;

namespace WebApiServer.Services.Soap
{
    /// <summary>
    /// SOAP service contract for AI text generation
    /// This defines the XML Web Service interface that clients can consume
    /// </summary>
    [ServiceContract(Namespace = "http://webapi.distributedlab.com/soap")]
    public interface IAiTextSoapService
    {
        /// <summary>
        /// Submits a text generation request via SOAP
        /// </summary>
        [OperationContract]
        SoapSubmitResponse SubmitTextRequest(SoapTextRequest request);

        /// <summary>
        /// Gets the status of a previously submitted request via SOAP
        /// </summary>
        [OperationContract]
        SoapStatusResponse GetRequestStatus(SoapStatusRequest request);
    }

    #region SOAP Data Contracts

    /// <summary>
    /// SOAP request for text generation
    /// </summary>
    [DataContract(Namespace = "http://webapi.distributedlab.com/soap")]
    public class SoapTextRequest
    {
        [DataMember]
        public string Prompt { get; set; } = string.Empty;

        [DataMember]
        public string? PreviousRequestId { get; set; }
    }

    /// <summary>
    /// SOAP response when submitting a request
    /// </summary>
    [DataContract(Namespace = "http://webapi.distributedlab.com/soap")]
    public class SoapSubmitResponse
    {
        [DataMember]
        public string RequestId { get; set; } = string.Empty;

        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public string SubmissionTime { get; set; } = string.Empty;

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// SOAP request for checking status
    /// </summary>
    [DataContract(Namespace = "http://webapi.distributedlab.com/soap")]
    public class SoapStatusRequest
    {
        [DataMember]
        public string RequestId { get; set; } = string.Empty;
    }

    /// <summary>
    /// SOAP response with request status and result
    /// </summary>
    [DataContract(Namespace = "http://webapi.distributedlab.com/soap")]
    public class SoapStatusResponse
    {
        [DataMember]
        public string RequestId { get; set; } = string.Empty;

        [DataMember]
        public string Status { get; set; } = string.Empty;

        [DataMember]
        public string? Result { get; set; }

        [DataMember]
        public string SubmissionTime { get; set; } = string.Empty;

        [DataMember]
        public string ClientPrompt { get; set; } = string.Empty;

        [DataMember]
        public string? PreviousRequestId { get; set; }

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string? ErrorMessage { get; set; }
    }

    #endregion
}