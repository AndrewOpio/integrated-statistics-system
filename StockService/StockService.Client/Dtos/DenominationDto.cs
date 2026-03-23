using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockService.Client.Dtos
{
    public class DenominationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? DenominationTypeId { get; set; }
        public decimal Value { get; set; }
        public int NumberOfPiecesInABox { get; set; }
        public DenominationTypeDto? DenominationType { get; set; }
        public bool IsDeleted { get; set; }
    }
}
