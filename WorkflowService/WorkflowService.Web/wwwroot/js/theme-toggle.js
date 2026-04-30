(function () {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');

    function getStoredTheme() {
        return localStorage.getItem('theme') || 'light';
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('theme', theme);
        updateIcon(theme);
    }

    function updateIcon(theme) {
        if (!themeIcon) return;
        if (theme === 'dark') {
            themeIcon.classList.remove('ti-sun');
            themeIcon.classList.add('ti-moon');
            themeToggleBtn.setAttribute('title', 'Switch to light mode');
        } else {
            themeIcon.classList.remove('ti-moon');
            themeIcon.classList.add('ti-sun');
            themeToggleBtn.setAttribute('title', 'Switch to dark mode');
        }
    }

    // Set icon on load based on current theme
    updateIcon(getStoredTheme());

    // Toggle on click
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            const current = getStoredTheme();
            const next = current === 'dark' ? 'light' : 'dark';
            setTheme(next);
        });
    }
})();