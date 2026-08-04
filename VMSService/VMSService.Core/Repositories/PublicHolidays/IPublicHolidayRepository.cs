using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PublicHolidays
{
    public interface IPublicHolidayRepository : IRepositoryBase<PublicHoliday, int>
    {
        Task<ICollection<PublicHoliday>> FindByActive(bool active);

        /// <summary>The active public holiday on the given date, if any — used to determine
        /// whether the day before a collection date was a non-working day.</summary>
        Task<PublicHoliday?> FindByDateAsync(DateOnly date);
    }
}
