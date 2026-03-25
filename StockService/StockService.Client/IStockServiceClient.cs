using StockService.Client.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockService.Client;
public interface IStockServiceClient
{
    Task<IEnumerable<DenominationDto>> GetAllDenominations();
}
