// Shared grace-window rule for same-day/exceptional approval, used by both the deposit
// and withdrawal forms. Generalizes "Monday morning" into "the day before is Saturday,
// Sunday, or a public holiday" — mirrors the server-side
// VMSService.Core.Services.Submissions.SubmissionClassifier's dayBeforeIsNonWorking check
// plus its 11:00 EAT cutoff, so both sides apply the same rule.
//
// Uganda (EAT) is a fixed UTC+3 offset with no daylight saving, so wall-clock EAT fields
// can be read directly off a UTC-shifted timestamp without relying on the browser's own
// timezone or locale-formatting quirks.
//
// publicHolidayDates: array of { date: 'yyyy-MM-dd', name: string } — see window.publicHolidayDates.

// Returns null when the grace period is not active, otherwise a human-readable reason
// ("the previous day was a weekend" or the specific holiday's name) for display.
function getGracePeriodReason(publicHolidayDates) {
    const EAT_OFFSET_MS = 3 * 60 * 60 * 1000;

    const nowEat = new Date(Date.now() + EAT_OFFSET_MS);
    const yesterdayEat = new Date(nowEat.getTime() - 24 * 60 * 60 * 1000);

    const yesterdayStr = yesterdayEat.toISOString().split('T')[0]; // yyyy-MM-dd
    const yesterdayDow = yesterdayEat.getUTCDay(); // 0 = Sunday, 6 = Saturday
    const currentHourEat = nowEat.getUTCHours();

    if (currentHourEat >= 11) return null;

    const holiday = (publicHolidayDates || []).find(h => h.date === yesterdayStr);
    if (holiday) return holiday.name;

    if (yesterdayDow === 0 || yesterdayDow === 6) return 'the previous day was a weekend';

    return null;
}

function isGracePeriodActive(publicHolidayDates) {
    return getGracePeriodReason(publicHolidayDates) !== null;
}
