(function () {
    // 🌐 Modal Root
    let modalRoot = document.getElementById("sharedModalRoot");
    if (!modalRoot) {
        modalRoot = document.createElement("div");
        modalRoot.id = "sharedModalRoot";
        document.body.appendChild(modalRoot);
    }

    const icons = {
        info: 'fa-info-circle',
        success: 'fa-check-circle',
        warning: 'fa-exclamation-triangle',
        error: 'fa-times-circle',
        confirm: 'fa-question-circle'
    };

    const colors = {
        info: 'bg-primary',
        success: 'bg-success',
        warning: 'bg-warning',
        error: 'bg-danger',
        confirm: 'bg-danger'
    };

    // Generic Modal Creator
    function createModal({ message, title = "Notice", type = "info", confirmText = "Yes", cancelText = "Cancel", onConfirm }) {
        const modalId = "sharedModal_" + Date.now();
        modalRoot.innerHTML = `
            <div class="modal fade shared-modal" id="${modalId}" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content shadow">
                        <div class="modal-header ${colors[type] || colors.info}">
                            <h5 class="modal-title"><i class="fas ${icons[type] || icons.info} me-2"></i>${title}</h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">${message}</div>
                        <div class="modal-footer">
                            ${type === 'confirm' ? `<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${cancelText}</button>` : ''}
                            <button type="button" class="btn ${type === 'success' || type === 'info' || type === 'warning' ? 'btn-primary' : 'btn-danger'}" id="confirmBtn">${type === 'confirm' ? confirmText : 'OK'}</button>
                        </div>
                    </div>
                </div>
            </div>`;

        const modalEl = document.getElementById(modalId);
        const modal = new bootstrap.Modal(modalEl);
        modal.show();

        const confirmBtn = modalEl.querySelector("#confirmBtn");
        confirmBtn.addEventListener("click", async () => {
            modal.hide();
            if (typeof onConfirm === "function") await onConfirm();
        });
    }

    // 🔹 Ready-to-use Modal Functions
    window.ShowModalInfo = (message, title = "Information") => createModal({ message, title, type: "info" });
    window.ShowModalSuccess = (message, title = "Success") => createModal({ message, title, type: "success" });
    window.ShowModalWarning = (message, title = "Warning") => createModal({ message, title, type: "warning" });
    window.ShowModalError = (message, title = "Error") => createModal({ message, title, type: "error" });
    window.ShowModalConfirm = (message, onConfirm, title = "Confirm Action", confirmText = "Yes", cancelText = "Cancel") =>
        createModal({ message, title, type: "confirm", confirmText, cancelText, onConfirm });
    window.ShowModalAlert = (message, title = 'Alert', type = 'info') => {
        if (type === 'success') return ShowModalSuccess(message, title);
        if (type === 'error') return ShowModalError(message, title);
        if (type === 'warning') return ShowModalWarning(message, title);
        return ShowModalInfo(message, title);
    };


})();
