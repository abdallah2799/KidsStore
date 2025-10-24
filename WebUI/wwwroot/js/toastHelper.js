// toastHelper.js
(function () {
    window.ShowToast = function (message, type = 'success', duration = 3000, position = 'bottom-right') {
        const icons = {
            success: '✔️',
            error: '❌',
            warning: '⚠️',
            info: 'ℹ️'
        };

        const typeStyles = {
            success: 'background: #16a34a; color: white;',
            error: 'background: #dc2626; color: white;',
            warning: 'background: #facc15; color: black;',
            info: 'background: #3b82f6; color: white;'
        };

        const positions = {
            'top-left': { top: '1rem', left: '1rem' },
            'top-right': { top: '1rem', right: '1rem' },
            'bottom-left': { bottom: '1rem', left: '1rem' },
            'bottom-right': { bottom: '1rem', right: '1rem' }
        };

        const toast = document.createElement('div');

        // Inline styles instead of relying on classes that may conflict
        toast.style.cssText = `
            position: fixed;
            min-width: 250px;
            padding: 0.75rem 1rem;
            border-radius: 0.5rem;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            display: flex;
            align-items: center;
            justify-content: space-between;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            z-index: 9999;
            opacity: 0;
            transform: translateX(${position.includes('right') ? '100%' : '-100%'});
            transition: transform 0.3s ease, opacity 0.3s ease;
            ${typeStyles[type] || typeStyles.info}
            ${Object.entries(positions[position] || positions['bottom-right']).map(([k, v]) => `${k}:${v}`).join(';')}
        `;

        toast.innerHTML = `
            <div style="display:flex; align-items:center; gap:0.5rem;">
                <span>${icons[type] || ''}</span>
                <span>${message}</span>
            </div>
            <button type="button" style="
                background: transparent; border: none; color: inherit; font-weight: bold; cursor: pointer;
            " aria-label="Close">&times;</button>
        `;

        document.body.appendChild(toast);

        // Force reflow before animation
        toast.getBoundingClientRect();
        requestAnimationFrame(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateX(0)';
        });

        const btn = toast.querySelector('button');
        btn.addEventListener('click', () => removeToast(toast));

        const timeoutId = setTimeout(() => removeToast(toast), duration);

        function removeToast(el) {
            clearTimeout(timeoutId);
            el.style.opacity = '0';
            el.style.transform = `translateX(${position.includes('right') ? '100%' : '-100%'})`;
            setTimeout(() => el.remove(), 300);
        }
    };
})();
