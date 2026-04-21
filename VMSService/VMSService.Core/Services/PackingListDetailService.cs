using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Repositories.PackingListRangeDetails;
using VMSService.Data.Entities;

namespace VMSService.Core.Services.PackingListRangeDetails
{
    public interface IPackingListDetailService
    {
        Task PopulateSerialNumbersAsync(int packingListRangeId);
    }

    public class PackingListDetailService(
        IPackingListRangeRepository _packingListRangeRepository,
        IPackingListDetailsRepository _packingListDetailsRepository) : IPackingListDetailService
    {
        public async Task PopulateSerialNumbersAsync(int packingListRangeId)
        {
            var range = await _packingListRangeRepository.FindByIdAsync(packingListRangeId);
            if (range == null)
                return;

            // Check if details already exist
            var existingDetails = await _packingListDetailsRepository.FindByPackingListRangeIdAsync(packingListRangeId);
            if (existingDetails.Count > 0)
                return; // Already populated

            var detailsList = new List<PackingListDetails>();
            const int serialsPerCase = 50000;
            
            int currentSerial = range.SNRangeStart;

            for (int i = 0; i <= (range.EndCaseNumber - range.StartCaseNumber); i++)
            {
                var caseNumber = range.StartCaseNumber + i;
                var serialStart = currentSerial;
                var serialEnd = currentSerial + serialsPerCase - 1;

                detailsList.Add(new PackingListDetails
                {
                    PackingListRangeId = packingListRangeId,
                    CaseNumber = caseNumber,
                    SerialNoteStartNumber = serialStart,
                    SerialNoteEndNumber = serialEnd
                });

                currentSerial = serialEnd + 1;
            }

            if (detailsList.Count > 0)
            {
                await _packingListDetailsRepository.CreateBatchAsync(detailsList);
            }
        }
    }
}
