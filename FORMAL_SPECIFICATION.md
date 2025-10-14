# Formal Specification: OpenAI Web API Service

## 1. The Formal Specification of the System

### System Aggregates and Connections
Aggregate(Client)
Aggregate(WebAPI)
Aggregate(AzureOpenAI_Service)

Connection(Client, WebAPI)
Connection(WebAPI, AzureOpenAI_Service)

### Client Signals
OutputSignal(Client, GENERATE_REQ, prompt, reqNo)
OutputSignal(Client, GET_STATUS, reqNo)
InputSignal(Client, REQUEST_ACCEPTED, reqNo)
InputSignal(Client, STATUS_UPDATE, reqNo, status, result)

### WebAPI Signals
InputSignal(WebAPI, GENERATE_REQ, prompt, reqNo)
InputSignal(WebAPI, GET_STATUS, reqNo)
OutputSignal(WebAPI, REQUEST_ACCEPTED, reqNo)
OutputSignal(WebAPI, STATUS_UPDATE, reqNo, status, result)
OutputSignal(WebAPI, OPENAI_REQUEST, prompt, reqNo)
InputSignal(WebAPI, OPENAI_RESPONSE, reqNo, result, success)

### AzureOpenAI_Service Signals
InputSignal(AzureOpenAI_Service, OPENAI_REQUEST, prompt, reqNo)
OutputSignal(AzureOpenAI_Service, OPENAI_RESPONSE, reqNo, result, success)

### Component States
State(WebAPI_Request, PENDING, PROCESSING, COMPLETE, FAILED)

### Production Rules

Rule 1: Request Submission
IF: OutputSignal(Client, GENERATE_REQ, prompt, reqNo) 
THEN: State(WebAPI_Request, PENDING) AND OutputSignal(WebAPI, REQUEST_ACCEPTED, reqNo)

Rule 2: Processing Begins  
IF: State(WebAPI_Request, PENDING) AND RateLimitSatisfied()
THEN: State(WebAPI_Request, PROCESSING) AND OutputSignal(WebAPI, OPENAI_REQUEST, prompt, reqNo)

Rule 3: Successful Response
IF: State(WebAPI_Request, PROCESSING) AND InputSignal(WebAPI, OPENAI_RESPONSE, reqNo, result, true)
THEN: State(WebAPI_Request, COMPLETE)

Rule 4: Failed Response
IF: State(WebAPI_Request, PROCESSING) AND InputSignal(WebAPI, OPENAI_RESPONSE, reqNo, error, false)  
THEN: State(WebAPI_Request, FAILED)

Rule 5: Status Polling
IF: OutputSignal(Client, GET_STATUS, reqNo) AND State(WebAPI_Request, status)
THEN: OutputSignal(WebAPI, STATUS_UPDATE, reqNo, status, result)

### Auxiliary Conditions
RateLimitSatisfied() := (CurrentTime - LastAPICall) >= 45_seconds