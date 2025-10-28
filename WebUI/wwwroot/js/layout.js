// 🧠 User dropdown toggle
document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("userDropdownToggle");
    const dropdown = document.getElementById("userDropdown");

    toggle.addEventListener("click", (e) => {
        e.stopPropagation();
        dropdown.classList.toggle("show");
    });

    document.addEventListener("click", (e) => {
        if (!dropdown.contains(e.target) && !toggle.contains(e.target)) {
            dropdown.classList.remove("show");
        }
    });
});

// 🎯 Sidebar collapse toggle
function toggleSidebarCollapse() {
    document.body.classList.toggle('sidebar-collapsed');
    localStorage.setItem('sidebarCollapsed', document.body.classList.contains('sidebar-collapsed'));
}

// 📱 Mobile sidebar toggle
function toggleSidebar() {
    document.body.classList.toggle('sidebar-open');
}

// Restore sidebar state on page load
document.addEventListener("DOMContentLoaded", () => {
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed) {
        document.body.classList.add('sidebar-collapsed');
    }
});

// 🌓 Dark Mode Toggle
document.addEventListener("DOMContentLoaded", () => {
    const themeToggle = document.getElementById('themeToggle');
    const themeIcon = document.getElementById('themeIcon');
    const themeText = document.getElementById('themeText');
    const body = document.body;

    // Check if elements exist
    if (!themeToggle || !themeIcon || !themeText) {
        return;
    }

    // Check for saved theme preference or default to 'light'
    const currentTheme = localStorage.getItem('theme') || 'light';
    
    // Apply saved theme on load
    if (currentTheme === 'dark') {
        body.setAttribute('data-theme', 'dark');
        themeToggle.checked = true;
        updateThemeUI(true);
    }

    // Toggle theme on switch change
    themeToggle.addEventListener('change', function() {
        if (this.checked) {
            body.setAttribute('data-theme', 'dark');
            localStorage.setItem('theme', 'dark');
            updateThemeUI(true);
            applyTableTheme(true);
        } else {
            body.removeAttribute('data-theme');
            localStorage.setItem('theme', 'light');
            updateThemeUI(false);
            applyTableTheme(false);
        }
    });

    function updateThemeUI(isDark) {
        if (isDark) {
            themeIcon.classList.remove('fa-moon');
            themeIcon.classList.add('fa-sun');
            themeText.textContent = 'Light Mode';
        } else {
            themeIcon.classList.remove('fa-sun');
            themeIcon.classList.add('fa-moon');
            themeText.textContent = 'Dark Mode';
        }
    }

    function applyTableTheme(isDark) {
        const tables = document.querySelectorAll('.table');
        tables.forEach(table => {
            if (isDark) {
                table.classList.add('table-dark');
            } else {
                table.classList.remove('table-dark');
            }
        });
    }

    // Apply table theme on initial load
    if (currentTheme === 'dark') {
        applyTableTheme(true);
    }
});
