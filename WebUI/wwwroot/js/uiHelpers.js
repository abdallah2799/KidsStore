/* uiHelpers.js - Unified UI Helpers using SweetAlert2 */

/**
 * Show a toast notification
 * @param {string} message - The message to display
 * @param {string} type - Type of toast: 'success', 'error', 'warning', 'info'
 * @param {number} duration - Duration in milliseconds (default: 3000)
 * @param {string} position - Position: 'top-end', 'top-start', 'bottom-end', 'bottom-start', 'center' (default: 'top-end')
 */
function showToast(message, type = 'success', duration = 3000, position = 'top-end') {
    const Toast = Swal.mixin({
        toast: true,
        position: position,
        showConfirmButton: false,
        timer: duration,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        },
        customClass: {
            popup: 'swal2-toast-custom'
        }
    });

    Toast.fire({
        icon: type,
        title: message
    });
}

/**
 * Show a confirmation dialog
 * @param {string} title - The title of the dialog
 * @param {string} message - The message to display
 * @param {string} confirmText - Text for confirm button (default: 'Yes')
 * @param {string} cancelText - Text for cancel button (default: 'Cancel')
 * @param {string} icon - Icon type: 'warning', 'error', 'success', 'info', 'question' (default: 'question')
 * @returns {Promise<boolean>} - Returns true if confirmed, false if cancelled
 */
async function showConfirm(title, message, confirmText = 'Yes', cancelText = 'Cancel', icon = 'question') {
    const result = await Swal.fire({
        title: title,
        html: message,
        icon: icon,
        showCancelButton: true,
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        reverseButtons: true,
        customClass: {
            confirmButton: 'btn btn-primary me-2',
            cancelButton: 'btn btn-secondary'
        },
        buttonsStyling: false
    });

    return result.isConfirmed;
}

/**
 * Show a success modal
 * @param {string} title - The title of the modal
 * @param {string} message - The message to display
 * @param {string} buttonText - Text for the button (default: 'OK')
 */
async function showSuccess(title, message, buttonText = 'OK') {
    await Swal.fire({
        title: title,
        html: message,
        icon: 'success',
        confirmButtonText: buttonText,
        customClass: {
            confirmButton: 'btn btn-success'
        },
        buttonsStyling: false
    });
}

/**
 * Show an error modal
 * @param {string} title - The title of the modal
 * @param {string} message - The message to display
 * @param {string} buttonText - Text for the button (default: 'OK')
 */
async function showError(title, message, buttonText = 'OK') {
    await Swal.fire({
        title: title,
        html: message,
        icon: 'error',
        confirmButtonText: buttonText,
        customClass: {
            confirmButton: 'btn btn-danger'
        },
        buttonsStyling: false
    });
}

/**
 * Show a warning modal
 * @param {string} title - The title of the modal
 * @param {string} message - The message to display
 * @param {string} buttonText - Text for the button (default: 'OK')
 */
async function showWarning(title, message, buttonText = 'OK') {
    await Swal.fire({
        title: title,
        html: message,
        icon: 'warning',
        confirmButtonText: buttonText,
        customClass: {
            confirmButton: 'btn btn-warning'
        },
        buttonsStyling: false
    });
}

/**
 * Show an info modal
 * @param {string} title - The title of the modal
 * @param {string} message - The message to display
 * @param {string} buttonText - Text for the button (default: 'OK')
 */
async function showInfo(title, message, buttonText = 'OK') {
    await Swal.fire({
        title: title,
        html: message,
        icon: 'info',
        confirmButtonText: buttonText,
        customClass: {
            confirmButton: 'btn btn-info'
        },
        buttonsStyling: false
    });
}

/**
 * Show a loading modal
 * @param {string} title - The title of the modal (default: 'Please wait...')
 * @param {string} message - The message to display (default: 'Processing your request')
 */
function showLoading(title = 'Please wait...', message = 'Processing your request') {
    Swal.fire({
        title: title,
        html: message,
        allowOutsideClick: false,
        allowEscapeKey: false,
        allowEnterKey: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
}

/**
 * Close the loading modal
 */
function closeLoading() {
    Swal.close();
}

/**
 * Show an input prompt
 * @param {string} title - The title of the prompt
 * @param {string} message - The message to display
 * @param {string} inputType - Type of input: 'text', 'email', 'password', 'number', 'tel', 'range', 'textarea', 'select', 'radio', 'checkbox'
 * @param {string} inputPlaceholder - Placeholder text for the input
 * @param {*} inputValue - Default value for the input
 * @param {Array} inputOptions - Options for select, radio, or checkbox (optional)
 * @param {string} confirmText - Text for confirm button (default: 'Submit')
 * @param {string} cancelText - Text for cancel button (default: 'Cancel')
 * @returns {Promise<*>} - Returns the input value if confirmed, null if cancelled
 */
async function showPrompt(title, message, inputType = 'text', inputPlaceholder = '', inputValue = '', inputOptions = null, confirmText = 'Submit', cancelText = 'Cancel') {
    const config = {
        title: title,
        html: message,
        input: inputType,
        inputPlaceholder: inputPlaceholder,
        inputValue: inputValue,
        showCancelButton: true,
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        reverseButtons: true,
        customClass: {
            confirmButton: 'btn btn-primary me-2',
            cancelButton: 'btn btn-secondary'
        },
        buttonsStyling: false,
        inputValidator: (value) => {
            if (!value) {
                return 'This field is required!';
            }
        }
    };

    if (inputOptions) {
        config.inputOptions = inputOptions;
    }

    const result = await Swal.fire(config);

    return result.isConfirmed ? result.value : null;
}

/**
 * Show a delete confirmation dialog
 * @param {string} itemName - The name of the item to delete
 * @returns {Promise<boolean>} - Returns true if confirmed, false if cancelled
 */
async function showDeleteConfirm(itemName = 'this item') {
    return await showConfirm(
        'Delete Confirmation',
        `Are you sure you want to delete <strong>${itemName}</strong>?<br>This action cannot be undone.`,
        'Yes, Delete',
        'Cancel',
        'warning'
    );
}

/**
 * Show a custom modal with HTML content
 * @param {string} title - The title of the modal
 * @param {string} htmlContent - The HTML content to display
 * @param {string} icon - Icon type: 'warning', 'error', 'success', 'info', 'question' (default: null)
 * @param {boolean} showCancelButton - Whether to show cancel button (default: false)
 * @param {string} confirmText - Text for confirm button (default: 'OK')
 * @param {string} cancelText - Text for cancel button (default: 'Cancel')
 * @returns {Promise<boolean>} - Returns true if confirmed, false if cancelled
 */
async function showCustomModal(title, htmlContent, icon = null, showCancelButton = false, confirmText = 'OK', cancelText = 'Cancel') {
    const result = await Swal.fire({
        title: title,
        html: htmlContent,
        icon: icon,
        showCancelButton: showCancelButton,
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        reverseButtons: true,
        customClass: {
            confirmButton: 'btn btn-primary me-2',
            cancelButton: 'btn btn-secondary'
        },
        buttonsStyling: false
    });

    return result.isConfirmed;
}

/**
 * Show a modal with a form (backwards compatibility with existing code)
 * @param {Object} config - Configuration object
 * @param {string} config.title - The title of the modal
 * @param {string} config.message - The message to display
 * @param {string} config.type - Type: 'info', 'success', 'warning', 'error', 'confirm'
 * @param {string} config.confirmText - Text for confirm button
 * @param {string} config.cancelText - Text for cancel button
 * @param {Function} config.onConfirm - Callback function when confirmed
 */
function createModal(config) {
    const {
        title = 'Notice',
        message,
        type = 'info',
        confirmText = 'OK',
        cancelText = 'Cancel',
        onConfirm
    } = config;

    const showCancel = type === 'confirm';
    const icon = type === 'confirm' ? 'question' : type;

    Swal.fire({
        title: title,
        html: message,
        icon: icon,
        showCancelButton: showCancel,
        confirmButtonText: confirmText,
        cancelButtonText: cancelText,
        reverseButtons: true,
        customClass: {
            confirmButton: 'btn btn-primary me-2',
            cancelButton: 'btn btn-secondary'
        },
        buttonsStyling: false
    }).then((result) => {
        if (result.isConfirmed && onConfirm) {
            onConfirm();
        }
    });
}

/**
 * Backwards compatibility: Show toast using old API
 * @param {string} message - The message to display
 * @param {string} type - Type: 'success', 'error', 'warning', 'info'
 * @param {number} duration - Duration in milliseconds
 */
function ShowToast(message, type = 'success', duration = 3000) {
    showToast(message, type, duration);
}

// Apply custom styling to match our color schema
const style = document.createElement('style');
style.textContent = `
    .swal2-popup {
        background-color: var(--surface) !important;
        color: var(--text-primary) !important;
        border: 1px solid var(--border) !important;
    }
    
    .swal2-title {
        color: var(--text-primary) !important;
    }
    
    .swal2-html-container {
        color: var(--text-secondary) !important;
    }
    
    .swal2-icon {
        border-color: currentColor !important;
    }
    
    .swal2-icon.swal2-success [class^='swal2-success-line'] {
        background-color: var(--success) !important;
    }
    
    .swal2-icon.swal2-success .swal2-success-ring {
        border-color: var(--success) !important;
    }
    
    .swal2-icon.swal2-error {
        border-color: var(--danger) !important;
        color: var(--danger) !important;
    }
    
    .swal2-icon.swal2-warning {
        border-color: var(--warning) !important;
        color: var(--warning) !important;
    }
    
    .swal2-icon.swal2-info {
        border-color: var(--info) !important;
        color: var(--info) !important;
    }
    
    .swal2-icon.swal2-question {
        border-color: var(--primary) !important;
        color: var(--primary) !important;
    }
    
    .swal2-toast-custom {
        background-color: var(--surface) !important;
        box-shadow: var(--shadow-lg) !important;
        border: 1px solid var(--border) !important;
    }
    
    .swal2-input, .swal2-textarea {
        background-color: var(--input-bg) !important;
        color: var(--text-primary) !important;
        border: 1px solid var(--input-border) !important;
    }
    
    .swal2-input:focus, .swal2-textarea:focus {
        border-color: var(--input-focus-border) !important;
        box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1) !important;
    }
`;
document.head.appendChild(style);

// Export for module usage if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        showToast,
        showConfirm,
        showSuccess,
        showError,
        showWarning,
        showInfo,
        showLoading,
        closeLoading,
        showPrompt,
        showDeleteConfirm,
        showCustomModal,
        createModal,
        ShowToast
    };
}
