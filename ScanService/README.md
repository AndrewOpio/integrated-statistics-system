# ScanService

Scans file uploads for the other Aswam microservices using **Windows Defender via AMSI**
(the Antimalware Scan Interface). A file is scanned **in memory** and only written to disk if the
antimalware engine clears it, so a malicious upload never lands on the file system.

## How a scan flows

1. A user uploads a file to a microservice (for example VMSService).
2. That service streams the upload straight to ScanService - it does not write it anywhere first
   (`IFormFile.OpenReadStream()` goes into a multipart request via `StreamContent`).
3. ScanService reads the multipart body itself with a `MultipartReader`, so ASP.NET Core does not
   spool the upload to a temp file, and holds the bytes in a `byte[]`.
4. `AmsiScanBuffer` is called on that buffer, inside an `AmsiOpenSession`/`AmsiCloseSession` pair.
5. AMSI forwards the content to the registered antimalware provider (Microsoft Defender).
6. The provider returns an `AMSI_RESULT`. Anything `>= 32768` is malware.
7. If clean, ScanService hands the cleared bytes back to the caller, which writes them to its own
   storage. (Or, with `/api/scan/store`, writes them itself to a directory the caller named.)
8. If malicious, nothing is written or returned and the response says the file was found to be
   malicious.

## Endpoints

| Method | Route               | Purpose                                                            |
| ------ | ------------------- | ------------------------------------------------------------------ |
| POST   | `/api/scan`         | Scan a file, return the verdict only.                              |
| POST   | `/api/scan/content` | Scan and, if clean, return the cleared bytes for the caller to store. |
| POST   | `/api/scan/store`   | Scan and, if clean, store at `destinationDirectory`.                |
| GET    | `/api/scan/health`  | Whether AMSI initialised on this host.                             |
| GET    | `/health`           | Health check - unhealthy when AMSI is unavailable.                 |
| GET    | `/alive`            | Liveness.                                                          |

All POST endpoints take `multipart/form-data`:

- `file` - the upload (required)
- `destinationDirectory` - absolute directory, `/api/scan/store` only (required)
- `destinationFileName` - optional; defaults to `{guid}_{originalName}`
- `overwrite` - optional, defaults to `false`

### Which storing endpoint to use

**`/api/scan/content` is the default choice**, and the only one that works when the scanner runs on
its own server. The scanner needs no access to the caller's storage: the caller receives the exact
buffer the engine cleared and writes it wherever it likes. That is what lets one ScanService
deployment serve every microservice in Aswam.

`/api/scan/store` only makes sense when this service can reach the destination itself - the same
host, or a share both hosts see. Point it at a path it cannot see and the file lands on the
*scanner's* disk, where the caller will never find it. See "Deploying on its own server" below.

### Responses

A completed scan returns **200** whether the verdict is clean or malicious - callers key off
`isClean`. Non-2xx means the request or the service failed: 400 (bad request, blocked extension,
disallowed destination), 413 (too large), 503 (could not be scanned, `FailClosed`).

`/api/scan/content` is the one endpoint with two response shapes. A clean file comes back as
`application/octet-stream` - the raw bytes, so nothing is base64 inflated - with the verdict in
headers (`X-Scan-Verdict`, `X-Scan-Amsi-Result`, `X-Scan-Correlation-Id`, `X-Scan-Provider`,
`X-Scan-Scanned-At`, names in `ScanHeaders`). Anything else is the usual JSON. The typed client
hides this: it returns one `FileScanResultDto` either way, with the bytes on `.Content`.

## Using it from another microservice

```csharp
// Program.cs
builder.Services.AddScanServiceClient(builder.Configuration["MicroServices:scanservice"]);

// Where a file used to be written straight to disk
await using var stream = file.OpenReadStream();
var result = await _scanClient.ScanAndGetContentAsync(stream, file.FileName, file.ContentType);

if (!result.IsClean || result.Content is null)
{
    ModelState.AddModelError("", result.Message);
    return View(model);
}

// result.Content is the exact buffer the engine cleared - write that, not your own copy.
var fileName = SafeFileName.Create(file.FileName);   // "{guid}_{original}", path stripped
var fullPath = Path.Combine(myUploadsFolder, fileName);
await File.WriteAllBytesAsync(fullPath, result.Content);

var webPath = $"/uploads/myfolder/{fileName}";
```

Write the bytes the scanner returned, never a second copy of the upload you kept for yourself -
otherwise what you scanned and what you saved are two different objects.

VMSService wraps this in `IScannedUploadService`, which scans a whole batch of form uploads and
rolls back files already written if a later one in the batch is rejected.

## Configuration

Copy `ScanService.Api/appsettings.Example.json` to `appsettings.json` (settings files are
gitignored across this repo) and set at least `ScanService:AllowedDestinationRoots` - it is the
list of directories the service will write into, and an empty list refuses every store request.

## Deploying on its own server

Use `/api/scan/content` and nothing else needs arranging: the scanner holds no storage, so it can
sit on a hardened box of its own and serve every microservice. Each caller writes its own files.

Set the caller's `MicroServices:scanservice` to the real host - VMSService falls back to
`https://localhost:7315` if the key is missing, which on a separate-server deployment means every
upload quietly fails closed. Open the port, make sure the caller trusts the scanner's certificate,
and raise the client timeout if the link is slow (the default is 3 minutes).

If you do use `/api/scan/store` across hosts, `destinationDirectory` is resolved on the **scanner's**
file system, so `C:\CASFiles\uploads` means the scanner's C: drive. Files land there, the caller's
`/uploads` path serves 404s, and nothing logs an error. Only use it with a UNC path both hosts see,
and grant the scanner's service account write access to it.

## Requirements and behaviour notes

- **Windows only.** AMSI is a Windows API and needs a registered antimalware provider. On any other
  host `AmsiInitialize` fails, the service logs a critical error, `/health` reports unhealthy, and
  (with the default `FailClosed: true`) every upload is rejected rather than silently accepted.
- `MaxFileSizeBytes` is also the per-scan memory cost, since the whole file is buffered to be
  scanned - and with `/api/scan/content` the caller holds a copy of that buffer too while it writes
  it. `MaxConcurrentScans` caps how many scans can be in flight at once.
- The scan client opts out of the standard resilience handler that Aswam hosts apply to every
  `HttpClient`: its 10 second per-attempt timeout is too short for a scan, and its retries would
  try to replay an upload stream that has already been consumed.

## Verifying it works

Post the [EICAR test string](https://www.eicar.org/download-anti-malware-testfile/) to
`/api/scan`. Build the string at runtime rather than saving it to a file - Defender will quarantine
the file as soon as it is written. A working setup returns `isClean: false`, `verdict: 1`
(Malicious) and `amsiResultCode: 32768`.
