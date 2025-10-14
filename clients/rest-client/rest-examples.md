# REST Client Examples

## Using HTTP files (VS Code REST Client extension)

### Submit Request
POST http://localhost:5166/api/generate
Content-Type: application/json

{
    "prompt": "Write a short story about REST APIs",
    "previousRequestId": null
}

### Check Status  
GET http://localhost:5166/api/status/{{requestId}}

---

## Using cURL

### Submit Request
```bash
curl -X POST http://localhost:5166/api/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Write a poem about HTTP",
    "previousRequestId": null
  }'
```

### Check Status
```bash
curl -X GET http://localhost:5166/api/status/YOUR_REQUEST_ID_HERE
```

---

## Using PowerShell

### Submit Request
```powershell
$body = @{
    prompt = "Explain REST in simple terms"
    previousRequestId = $null
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5166/api/generate" -Method Post -Body $body -ContentType "application/json"
Write-Host "Request ID: $($response.requestId)"
```

### Check Status  
```powershell
$requestId = "YOUR_REQUEST_ID_HERE"
$status = Invoke-RestMethod -Uri "http://localhost:5166/api/status/$requestId" -Method Get
Write-Host "Status: $($status.status)"
if ($status.status -eq "COMPLETE") {
    Write-Host "Result: $($status.result)"
}
```