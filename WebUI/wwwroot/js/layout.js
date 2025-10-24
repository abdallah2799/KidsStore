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
