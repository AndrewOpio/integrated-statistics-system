using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EncryptionController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionController(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    // POST api/encryption/encrypt
    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] EncryptRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var encrypted = _encryptionService.Encrypt(request.Amount);

        return Ok(new
        {
            OriginalAmount = request.Amount,
            EncryptedValue = encrypted
        });
    }

    // POST api/encryption/decrypt
    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] DecryptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EncryptedValue))
            return BadRequest("Encrypted value cannot be empty.");

        var decrypted = _encryptionService.Decrypt(request.EncryptedValue);

        return Ok(new
        {
            EncryptedValue = request.EncryptedValue,
            DecryptedAmount = decrypted
        });
    }

    // POST api/encryption/roundtrip
    [HttpPost("roundtrip")]
    public IActionResult RoundTrip([FromBody] EncryptRequest request)
    {
        if (request.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var encrypted = _encryptionService.Encrypt(request.Amount);
        var decrypted = _encryptionService.Decrypt(encrypted);

        return Ok(new
        {
            OriginalAmount = request.Amount,
            EncryptedValue = encrypted,
            DecryptedAmount = decrypted,
            Match = request.Amount == decrypted
        });
    }

    // GET api/encryption/generatekey
    [HttpGet("generatekey")]
    public IActionResult GenerateKey()
    {
        var key = new byte[32]; // 256-bit
        RandomNumberGenerator.Fill(key);

        return Ok(new
        {
            Key = Convert.ToBase64String(key),
            Note = "Store this key safely. Never expose this endpoint in production."
        });
    }
}

// Request models
public class EncryptRequest
{
    public decimal Amount { get; set; }
}

public class DecryptRequest
{
    public string EncryptedValue { get; set; }
}