/*
 * Session expiry handling, shared by every Aswam web service.
 *
 * The auth token is short-lived. Rather than dropping people at the login page when it
 * runs out, this does one of two things:
 *
 *   - user has been active recently  -> renew silently in the background, no interruption
 *   - user has gone idle             -> show a countdown asking them to confirm
 *
 * Reads its configuration from the #session-timeout element rendered by _SessionTimeout.cshtml.
 */
(function () {
    'use strict';

    var host = document.getElementById('session-timeout');
    if (!host) {
        return;
    }

    var config = {
        statusUrl: host.dataset.statusUrl,
        extendUrl: host.dataset.extendUrl,
        endUrl: host.dataset.endUrl,
        // Treat the user as "active" if they did anything in this window.
        idleThresholdSeconds: parseInt(host.dataset.idleThresholdSeconds, 10) || 120,
        // Start renewing this far ahead of expiry, so a renewal has room to fail and retry.
        renewBeforeSeconds: parseInt(host.dataset.renewBeforeSeconds, 10) || 300,
        // Show the prompt this far ahead of expiry.
        warnBeforeSeconds: parseInt(host.dataset.warnBeforeSeconds, 10) || 120,
        pollIntervalSeconds: parseInt(host.dataset.pollIntervalSeconds, 10) || 5
    };

    var BROADCAST_KEY = 'aswam.session.expiresAt';

    var expiresAt = null;       // epoch ms
    var lastActivity = Date.now();
    var warningVisible = false;
    var renewInFlight = false;
    var finished = false;

    var modal = document.getElementById('session-timeout-modal');
    var countdownLabel = document.getElementById('session-timeout-countdown');
    var stayButton = document.getElementById('session-timeout-stay');
    var logoutButton = document.getElementById('session-timeout-logout');
    var errorLabel = document.getElementById('session-timeout-error');

    function antiForgeryToken() {
        var field = host.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : '';
    }

    function secondsRemaining() {
        if (!expiresAt) {
            return null;
        }
        return Math.floor((expiresAt - Date.now()) / 1000);
    }

    function isIdle() {
        return (Date.now() - lastActivity) / 1000 > config.idleThresholdSeconds;
    }

    function setExpiry(secondsFromNow, broadcast) {
        expiresAt = Date.now() + (secondsFromNow * 1000);

        if (broadcast) {
            // Keep other tabs of this service in step so they do not each renew.
            try {
                localStorage.setItem(BROADCAST_KEY, String(expiresAt));
            } catch (e) {
                /* private browsing, storage disabled - not worth failing over */
            }
        }
    }

    function showWarning() {
        if (warningVisible || !modal) {
            return;
        }
        warningVisible = true;
        if (errorLabel) {
            errorLabel.textContent = '';
            errorLabel.hidden = true;
        }
        modal.hidden = false;
        document.body.classList.add('session-timeout-open');
        if (stayButton) {
            stayButton.focus();
        }
    }

    function hideWarning() {
        warningVisible = false;
        if (modal) {
            modal.hidden = true;
        }
        document.body.classList.remove('session-timeout-open');
    }

    function endSession(timedOut) {
        if (finished) {
            return;
        }
        finished = true;
        window.location.href = config.endUrl + (timedOut ? '?timedOut=true' : '');
    }

    function formatCountdown(seconds) {
        var safe = Math.max(0, seconds);
        var minutes = Math.floor(safe / 60);
        var remainder = safe % 60;
        return minutes + ':' + (remainder < 10 ? '0' : '') + remainder;
    }

    function extend(isSilent) {
        if (renewInFlight) {
            return;
        }
        renewInFlight = true;

        if (!isSilent && stayButton) {
            stayButton.disabled = true;
        }

        fetch(config.extendUrl, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'RequestVerificationToken': antiForgeryToken(),
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                renewInFlight = false;
                if (stayButton) {
                    stayButton.disabled = false;
                }

                if (result && result.success) {
                    setExpiry(result.secondsRemaining, true);
                    hideWarning();
                    return;
                }

                // Cannot be extended - usually the 8 hour cap. Tell the user why, and let
                // the countdown run out rather than yanking the page away mid-sentence.
                if (!isSilent && errorLabel) {
                    errorLabel.textContent = (result && result.message) || 'Your session could not be extended.';
                    errorLabel.hidden = false;
                } else if (isSilent) {
                    showWarning();
                }
            })
            .catch(function () {
                renewInFlight = false;
                if (stayButton) {
                    stayButton.disabled = false;
                }
                if (!isSilent && errorLabel) {
                    errorLabel.textContent = 'Could not reach the server. Check your connection and try again.';
                    errorLabel.hidden = false;
                }
            });
    }

    function tick() {
        if (finished) {
            return;
        }

        var remaining = secondsRemaining();
        if (remaining === null) {
            return;
        }

        if (remaining <= 0) {
            endSession(true);
            return;
        }

        if (warningVisible) {
            if (countdownLabel) {
                countdownLabel.textContent = formatCountdown(remaining);
            }
            return;
        }

        if (isIdle()) {
            if (remaining <= config.warnBeforeSeconds) {
                if (countdownLabel) {
                    countdownLabel.textContent = formatCountdown(remaining);
                }
                showWarning();
            }
            return;
        }

        // Active user: keep them signed in without saying anything.
        if (remaining <= config.renewBeforeSeconds) {
            extend(true);
        }
    }

    function recordActivity() {
        lastActivity = Date.now();
    }

    ['mousedown', 'keydown', 'scroll', 'touchstart', 'click'].forEach(function (event) {
        document.addEventListener(event, recordActivity, { passive: true });
    });

    // A tab that was in the background may have missed ticks; re-check on return.
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) {
            recordActivity();
            tick();
        }
    });

    window.addEventListener('storage', function (event) {
        if (event.key === BROADCAST_KEY && event.newValue) {
            var broadcast = parseInt(event.newValue, 10);
            if (!isNaN(broadcast) && broadcast > (expiresAt || 0)) {
                expiresAt = broadcast;
                hideWarning();
            }
        }
    });

    if (stayButton) {
        stayButton.addEventListener('click', function () {
            recordActivity();
            extend(false);
        });
    }

    if (logoutButton) {
        logoutButton.addEventListener('click', function () {
            endSession(false);
        });
    }

    fetch(config.statusUrl, { credentials: 'same-origin' })
        .then(function (response) { return response.json(); })
        .then(function (status) {
            if (!status || !status.authenticated) {
                return;
            }
            setExpiry(status.secondsRemaining, false);
            setInterval(tick, config.pollIntervalSeconds * 1000);
            tick();
        })
        .catch(function () { /* not signed in, or status unreachable - stay out of the way */ });
})();
