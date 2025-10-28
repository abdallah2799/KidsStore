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
