using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Services.Stocks
{
    public class StockService : IStockService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockService> _logger;
        private readonly IConfiguration _configuration;
        private string _cachedToken;
        private DateTime _tokenExpiryTime;
        private readonly StockAPISettings _stockAPISettings;

        public StockService(HttpClient httpClient, ILogger<StockService> logger, IOptions<StockAPISettings> stockAPISettings, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _cachedToken = null;
            _tokenExpiryTime = DateTime.MinValue;
            _stockAPISettings = stockAPISettings.Value;
        }

        /// <summary>
        /// Gets or refreshes the authentication token from the API
        /// </summary>
        private async Task<string> GetAuthTokenAsync()
        {
            try
            {
                // Return cached token if still valid
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime)
                {
                    _logger.LogInformation("Using cached authentication token");
                    return _cachedToken;
                }

                //var baseUrl = _stockAPISettings.BaseUrl;
                //var tokenEndpoint = _configuration["StockApiSettings:TokenEndpoint"];
                //var clientId = _configuration["StockApiSettings:ClientId"];
                //var clientSecret = _configuration["StockApiSettings:ClientSecret"];
                //var cacheDuration = int.Parse(_configuration["StockApiSettings:TokenCacheDurationMinutes"] ?? "14");

                var baseUrl = _stockAPISettings.BaseUrl;
                var tokenEndpoint = _stockAPISettings.TokenEndpoint;
                var clientId = _stockAPISettings.ClientId;
                var clientSecret = _stockAPISettings.ClientSecret;
                var cacheDuration = int.Parse(_stockAPISettings.TokenCacheDurationMinutes ?? "14");

                var tokenUrl = $"{baseUrl}{tokenEndpoint}";

                var requestBody = new
                {
                    clientId = clientId,
                    clientSecret = clientSecret
                };

                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation("Requesting authentication token from {TokenUrl}", tokenUrl);

                var response = await _httpClient.PostAsync(tokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to obtain authentication token. Status: {StatusCode}, Message: {ErrorMessage}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Failed to obtain token. Status code: {response.StatusCode}");
                }

                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _logger.LogError("Invalid token response received from API");
                    throw new InvalidOperationException("Invalid token response from API");
                }

                // Cache the token
                _cachedToken = tokenResponse.AccessToken;
                _tokenExpiryTime = DateTime.UtcNow.AddMinutes(cacheDuration);

                _logger.LogInformation("Successfully obtained authentication token. Expires at: {ExpiryTime}", _tokenExpiryTime);

                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining authentication token from API");
                throw;
            }
        }

        /// <summary>
        /// Ensures the HttpClient has the authorization header set with the current token
        /// </summary>
        private async Task EnsureAuthorizationHeaderAsync()
        {
            var token = await GetAuthTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        
        #region DenominationType Methods

        public async Task<ICollection<DenominationType>> GetAllDenominationTypes()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/denominationtypes/get-all");

                if (response.IsSuccessStatusCode)
                {
                    var denominationTypes = await response.Content.ReadFromJsonAsync<ICollection<DenominationType>>();
                    return denominationTypes ?? new List<DenominationType>();
                }

                _logger.LogError("Failed to retrieve denomination types. Status: {StatusCode}", response.StatusCode);
                return new List<DenominationType>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination types from API");
                return new List<DenominationType>();
            }
        }

        public async Task<DenominationType> GetDenominationTypeById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/denominationtypes/get/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DenominationType>();
                }

                _logger.LogWarning("Denomination type with id {DenominationTypeId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination type {DenominationTypeId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateDenominationType(DenominationType denominationType)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/denominationtypes/create", denominationType);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Denomination type created successfully");
                    return true;
                }

                _logger.LogError("Failed to create denomination type. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination type via API");
                return false;
            }
        }

        public async Task<bool> UpdateDenominationType(DenominationType denominationType)
        {
            denominationType.Denominations = []; // Ensure we don't send the nested collection if not needed
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/denominationtypes/update/{denominationType.Id}", denominationType);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Denomination type updated successfully");
                    return true;
                }

                _logger.LogError("Failed to update denomination type. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination type via API");
                return false;
            }
        }

        #endregion

        #region Denomination Methods

        public async Task<ICollection<Denomination>> GetAllDenominations()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/denominations/get-all");

                //var errorContent = await response.Content.ReadAsStringAsync();
                //_logger.LogError("Failed Status: {StatusCode}, Response: {ErrorContent}",
                //    response.StatusCode, errorContent);

                if (response.IsSuccessStatusCode)
                {
                    var denominations = await response.Content.ReadFromJsonAsync<ICollection<Denomination>>();
                    return denominations ?? new List<Denomination>();
                }

                _logger.LogError("Failed to retrieve denominations. Status: {StatusCode}", response.StatusCode);
                return new List<Denomination>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denominations from API");
                return new List<Denomination>();
            }
        }

        public async Task<Denomination> GetDenominationById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/denominations/get/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Denomination>();
                }

                _logger.LogWarning("Denomination with id {DenominationId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving denomination {DenominationId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateDenomination(Denomination denomination)
        {
            denomination.DenominationType = null; // Ensure we don't send the nested object if not needed
            denomination.StockLedgerDetails = [];
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/denominations/create", denomination);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Denomination created successfully");
                    return true;
                }

                _logger.LogError("Failed to create denomination. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating denomination via API");
                return false;
            }
        }

        public async Task<bool> UpdateDenomination(Denomination denomination)
        {
            denomination.DenominationType = null; // Ensure we don't send the nested object if not needed
            denomination.StockLedgerDetails = []; 
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/denominations/update/{denomination.Id}", denomination);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Denomination updated successfully");
                    return true;
                }

                _logger.LogError("Failed to update denomination. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating denomination via API");
                return false;
            }
        }

        #endregion

        #region StockLedger Methods

        public async Task<ICollection<StockLedger>> GetAllStockLedgers()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/stockledgers/get-all");

                //var errorContent = await response.Content.ReadAsStringAsync();
                //_logger.LogError("Failed to create stock ledger. Status: {StatusCode}, Response: {ErrorContent}",
                //    response.StatusCode, errorContent);

                if (response.IsSuccessStatusCode)
                {
                    var stockLedgers = await response.Content.ReadFromJsonAsync<ICollection<StockLedger>>();
                    return stockLedgers ?? new List<StockLedger>();
                }
                
                _logger.LogError("Failed to retrieve stock ledgers. Status: {StatusCode}", response.StatusCode);
                return new List<StockLedger>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledgers from API");
                return new List<StockLedger>();
            }
        }

        public async Task<StockLedger> GetStockLedgerById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/stockledgers/get/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<StockLedger>();
                }
                
                _logger.LogWarning("Stock ledger with id {StockLedgerId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledger {StockLedgerId} from API", id);
                return null;
            }
        }

        public async Task<StockLedger> CreateStockLedger(StockLedger stockLedger)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/stockledgers/create", stockLedger);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Stock ledger created successfully");
                    return await response.Content.ReadFromJsonAsync<StockLedger>();
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create stock ledger. Status: {StatusCode}, Response: {ErrorContent}",
                    response.StatusCode, errorContent);

                _logger.LogError("Failed to create stock ledger. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock ledger via API");
                return null;
            }
        }

        #endregion

        #region StockLedgerDetail Methods

        public async Task<ICollection<StockLedgerDetail>> GetAllStockLedgerDetails()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                var response = await _httpClient.GetAsync($"{baseUrl}/api/stockledgerdetails");
                
                if (response.IsSuccessStatusCode)
                {
                    var stockLedgerDetails = await response.Content.ReadFromJsonAsync<ICollection<StockLedgerDetail>>();
                    return stockLedgerDetails ?? new List<StockLedgerDetail>();
                }
                
                _logger.LogError("Failed to retrieve stock ledger details. Status: {StatusCode}", response.StatusCode);
                return new List<StockLedgerDetail>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledger details from API");
                return new List<StockLedgerDetail>();
            }
        }

        public async Task<StockLedgerDetail> GetStockLedgerDetailById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                var response = await _httpClient.GetAsync($"{baseUrl}/api/stockledgerdetails/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<StockLedgerDetail>();
                }
                
                _logger.LogWarning("Stock ledger detail with id {StockLedgerDetailId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledger detail {StockLedgerDetailId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateStockLedgerDetail(StockLedgerDetail stockLedgerDetail)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/stockledgerdetails", stockLedgerDetail);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Stock ledger detail created successfully");
                    return true;
                }
                
                _logger.LogError("Failed to create stock ledger detail. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock ledger detail via API");
                return false;
            }
        }

        public async Task<bool> UpdateStockLedgerDetail(StockLedgerDetail stockLedgerDetail)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/stockledgerdetails/{stockLedgerDetail.Id}", stockLedgerDetail);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Stock ledger detail updated successfully");
                    return true;
                }
                
                _logger.LogError("Failed to update stock ledger detail. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock ledger detail via API");
                return false;
            }
        }

        #endregion

        #region Category Methods

        public async Task<ICollection<Category>> GetAllCategories()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/categories/get-all");

                if (response.IsSuccessStatusCode)
                {
                    var categories = await response.Content.ReadFromJsonAsync<ICollection<Category>>();
                    return categories ?? new List<Category>();
                }

                _logger.LogError("Failed to retrieve categories. Status: {StatusCode}", response.StatusCode);
                return new List<Category>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories from API");
                return new List<Category>();
            }
        }

        public async Task<Category> GetCategoryById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/categories/get/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Category>();
                }

                _logger.LogWarning("Category with id {CategoryId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category {CategoryId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateCategory(Category category)
        {
            category.BranchCategories = [];
            category.StockLedgers = [];
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/categories/create", category);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Category created successfully");
                    return true;
                }

                _logger.LogError("Failed to create category. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category via API");
                return false;
            }
        }

        public async Task<bool> UpdateCategory(Category category)
        {
            category.BranchCategories = [];
            category.StockLedgers = [];
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/categories/update/{category.Id}", category);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Category updated successfully");
                    return true;
                }

                _logger.LogError("Failed to update category. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category via API");
                return false;
            }
        }

        #endregion

        #region BranchCategory Methods

        public async Task<ICollection<BranchCategory>> GetAllBranchCategories()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/branchcategories/get-all");

                if (response.IsSuccessStatusCode)
                {
                    var branchCategories = await response.Content.ReadFromJsonAsync<ICollection<BranchCategory>>();
                    return branchCategories ?? new List<BranchCategory>();
                }

                _logger.LogError("Failed to retrieve branch categories. Status: {StatusCode}", response.StatusCode);
                return new List<BranchCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch categories from API");
                return new List<BranchCategory>();
            }
        }

        public async Task<BranchCategory> GetBranchCategoryById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/branchcategories/get/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<BranchCategory>();
                }

                _logger.LogWarning("Branch category with id {BranchCategoryId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branch category {BranchCategoryId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateBranchCategory(BranchCategory branchCategory)
        {
            branchCategory.Category = null;
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/branchcategories/create", branchCategory);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Branch category created successfully");
                    return true;
                }

                _logger.LogError("Failed to create branch category. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch category via API");
                return false;
            }
        }

        public async Task<bool> UpdateBranchCategory(BranchCategory branchCategory)
        {
            branchCategory.Category = null;
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/branchcategories/update/{branchCategory.Id}", branchCategory);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Branch category updated successfully");
                    return true;
                }

                _logger.LogError("Failed to update branch category. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch category via API");
                return false;
            }
        }

        #endregion

        #region CounterPartyType Methods

        public async Task<ICollection<CounterPartyType>> GetAllCounterPartyTypes()
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/counterpartytypes/get-all");

                if (response.IsSuccessStatusCode)
                {
                    var counterPartyTypes = await response.Content.ReadFromJsonAsync<ICollection<CounterPartyType>>();
                    return counterPartyTypes ?? new List<CounterPartyType>();
                }

                _logger.LogError("Failed to retrieve counter party types. Status: {StatusCode}", response.StatusCode);
                return new List<CounterPartyType>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party types from API");
                return new List<CounterPartyType>();
            }
        }

        public async Task<CounterPartyType> GetCounterPartyTypeById(int id)
        {
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.GetAsync($"{baseUrl}/api/counterpartytypes/get/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CounterPartyType>();
                }

                _logger.LogWarning("Counter party type with id {CounterPartyTypeId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving counter party type {CounterPartyTypeId} from API", id);
                return null;
            }
        }

        public async Task<bool> CreateCounterPartyType(CounterPartyType counterPartyType)
        {
            counterPartyType.StockLedgers = [];
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/api/counterpartytypes/create", counterPartyType);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Counter party type created successfully");
                    return true;
                }

                _logger.LogError("Failed to create counter party type. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating counter party type via API");
                return false;
            }
        }

        public async Task<bool> UpdateCounterPartyType(CounterPartyType counterPartyType)
        {
            counterPartyType.StockLedgers = [];
            try
            {
                await EnsureAuthorizationHeaderAsync();
                var baseUrl = _stockAPISettings.BaseUrl;
                var response = await _httpClient.PutAsJsonAsync($"{baseUrl}/api/counterpartytypes/update/{counterPartyType.Id}", counterPartyType);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Counter party type updated successfully");
                    return true;
                }

                _logger.LogError("Failed to update counter party type. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating counter party type via API");
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Model for token response from the API
    /// </summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }

    public class StockAPISettings
    {
        public string BaseUrl { get; set; }
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TokenCacheDurationMinutes { get; set; }
    
        
    }
}
