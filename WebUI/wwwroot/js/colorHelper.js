// ==================== COLOR HELPER ====================
// Centralized color management for the application

const COLORS = [
    { name: 'Red', hex: '#FF0000' },
    { name: 'Dark Red', hex: '#8B0000' },
    { name: 'Crimson', hex: '#DC143C' },
    { name: 'Pink', hex: '#FFC0CB' },
    { name: 'Hot Pink', hex: '#FF69B4' },
    { name: 'Deep Pink', hex: '#FF1493' },
    { name: 'Orange', hex: '#FFA500' },
    { name: 'Dark Orange', hex: '#FF8C00' },
    { name: 'Coral', hex: '#FF7F50' },
    { name: 'Yellow', hex: '#FFFF00' },
    { name: 'Gold', hex: '#FFD700' },
    { name: 'Light Yellow', hex: '#FFFFE0' },
    { name: 'Green', hex: '#008000' },
    { name: 'Dark Green', hex: '#006400' },
    { name: 'Light Green', hex: '#90EE90' },
    { name: 'Lime', hex: '#00FF00' },
    { name: 'Olive', hex: '#808000' },
    { name: 'Blue', hex: '#0000FF' },
    { name: 'Dark Blue', hex: '#00008B' },
    { name: 'Light Blue', hex: '#ADD8E6' },
    { name: 'Sky Blue', hex: '#87CEEB' },
    { name: 'Navy', hex: '#000080' },
    { name: 'Teal', hex: '#008080' },
    { name: 'Cyan', hex: '#00FFFF' },
    { name: 'Purple', hex: '#800080' },
    { name: 'Violet', hex: '#EE82EE' },
    { name: 'Indigo', hex: '#4B0082' },
    { name: 'Magenta', hex: '#FF00FF' },
    { name: 'Lavender', hex: '#E6E6FA' },
    { name: 'Brown', hex: '#A52A2A' },
    { name: 'Maroon', hex: '#800000' },
    { name: 'Beige', hex: '#F5F5DC' },
    { name: 'Tan', hex: '#D2B48C' },
    { name: 'Khaki', hex: '#F0E68C' },
    { name: 'Gray', hex: '#808080' },
    { name: 'Dark Gray', hex: '#A9A9A9' },
    { name: 'Light Gray', hex: '#D3D3D3' },
    { name: 'Silver', hex: '#C0C0C0' },
    { name: 'White', hex: '#FFFFFF' },
    { name: 'Black', hex: '#000000' },
    { name: 'Ivory', hex: '#FFFFF0' },
    { name: 'Cream', hex: '#FFFDD0' },
    { name: 'Mint', hex: '#98FF98' },
    { name: 'Peach', hex: '#FFDAB9' },
    { name: 'Turquoise', hex: '#40E0D0' },
    { name: 'Aqua', hex: '#00FFFF' },
    { name: 'Salmon', hex: '#FA8072' }
];

/**
 * Get color name from hex value
 * @param {string} hex - Hex color code
 * @returns {string} Color name or hex if not found
 */
function getColorNameFromHex(hex) {
    if (!hex) return '';
    const color = COLORS.find(c => c.hex.toLowerCase() === hex.toLowerCase());
    return color ? color.name : hex;
}

/**
 * Get hex value from color name
 * @param {string} name - Color name
 * @returns {string} Hex color code or empty string if not found
 */
function getColorHexFromName(name) {
    if (!name) return '';
    const color = COLORS.find(c => c.name.toLowerCase() === name.toLowerCase());
    return color ? color.hex : '';
}

/**
 * Get all color names
 * @returns {Array<string>} Array of color names
 */
function getAllColorNames() {
    return COLORS.map(c => c.name);
}

/**
 * Get all colors (name and hex)
 * @returns {Array<Object>} Array of color objects
 */
function getAllColors() {
    return [...COLORS];
}

/**
 * Setup color autocomplete for an input element with color palette
 * @param {string} inputSelector - CSS selector for the input element
 * @param {string} dropdownSelector - CSS selector for the dropdown element
 * @param {string} hiddenHexSelector - CSS selector for hidden input to store hex value
 */
function setupColorAutocomplete(inputSelector, dropdownSelector, hiddenHexSelector) {
    const input = document.querySelector(inputSelector);
    const dropdown = document.querySelector(dropdownSelector);
    const hiddenHex = document.querySelector(hiddenHexSelector);

    if (!input || !dropdown || !hiddenHex) return;

    // Show palette on focus
    input.addEventListener('focus', function () {
        showColorPalette(dropdown, input, hiddenHex);
    });

    // Filter palette on input
    input.addEventListener('input', function () {
        const query = this.value.toLowerCase().trim();
        
        if (query.length < 1) {
            showColorPalette(dropdown, input, hiddenHex);
            return;
        }

        const filtered = COLORS.filter(c =>
            c.name.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '<div class="p-2 text-muted text-center">No colors found</div>';
            dropdown.classList.add('show');
            return;
        }

        renderColorPalette(dropdown, filtered, input, hiddenHex);
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest(inputSelector) && !e.target.closest(dropdownSelector)) {
            dropdown.classList.remove('show');
        }
    });
}

/**
 * Show the full color palette
 */
function showColorPalette(dropdown, input, hiddenHex) {
    renderColorPalette(dropdown, COLORS, input, hiddenHex);
}

/**
 * Render color palette grid
 */
function renderColorPalette(dropdown, colors, input, hiddenHex) {
    dropdown.innerHTML = '';
    dropdown.classList.add('show');
    
    // Add search hint
    const hint = document.createElement('div');
    hint.className = 'p-2 text-muted small border-bottom';
    hint.innerHTML = '<i class="fas fa-search me-1"></i> Type to filter colors';
    dropdown.appendChild(hint);
    
    // Create palette grid container
    const paletteGrid = document.createElement('div');
    paletteGrid.className = 'color-palette-grid';
    
    colors.forEach(color => {
        const colorItem = document.createElement('div');
        colorItem.className = 'color-palette-item';
        colorItem.title = color.name;
        colorItem.style.backgroundColor = color.hex;
        
        // Add color name tooltip on hover
        const colorLabel = document.createElement('div');
        colorLabel.className = 'color-palette-label';
        colorLabel.textContent = color.name;
        
        colorItem.appendChild(colorLabel);
        
        colorItem.addEventListener('click', function () {
            input.value = color.name;
            hiddenHex.value = color.hex;
            dropdown.classList.remove('show');
            
            // Trigger change event for any listeners
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        
        paletteGrid.appendChild(colorItem);
    });
    
    dropdown.appendChild(paletteGrid);
}

/**
 * Create a color input group with autocomplete and palette
 * @param {string} id - Unique ID for this color input
 * @param {string} initialColor - Initial color name (optional)
 * @returns {Object} Object containing container element and getValue function
 */
function createColorInput(id, initialColor = '') {
    const container = document.createElement('div');
    container.className = 'position-relative';
    
    const inputGroup = document.createElement('div');
    inputGroup.className = 'input-group';
    
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'form-control color-input';
    input.placeholder = 'Select or type color...';
    input.id = `color-input-${id}`;
    input.value = initialColor;
    input.style.cursor = 'text';
    
    // Color preview swatch
    const swatchAddon = document.createElement('span');
    swatchAddon.className = 'input-group-text p-1';
    swatchAddon.style.width = '45px';
    swatchAddon.style.cursor = 'pointer';
    swatchAddon.title = 'Click to open color palette';
    
    const swatch = document.createElement('span');
    swatch.className = 'color-swatch d-block w-100 h-100';
    swatch.style.borderRadius = '3px';
    if (initialColor) {
        swatch.style.backgroundColor = getColorHexFromName(initialColor);
    } else {
        swatch.style.backgroundColor = '#fff';
    }
    
    swatchAddon.appendChild(swatch);
    
    const dropdown = document.createElement('div');
    dropdown.className = 'dropdown-menu';
    dropdown.id = `color-dropdown-${id}`;
    
    const hiddenHex = document.createElement('input');
    hiddenHex.type = 'hidden';
    hiddenHex.className = 'color-hex';
    hiddenHex.id = `color-hex-${id}`;
    
    // Set initial hex if color name provided
    if (initialColor) {
        hiddenHex.value = getColorHexFromName(initialColor);
    }
    
    inputGroup.appendChild(input);
    inputGroup.appendChild(swatchAddon);
    container.appendChild(inputGroup);
    container.appendChild(dropdown);
    container.appendChild(hiddenHex);
    
    // Click swatch to open palette
    swatchAddon.addEventListener('click', function(e) {
        e.stopPropagation();
        input.focus();
    });
    
    // Update swatch when color changes
    input.addEventListener('change', function() {
        const hex = getColorHexFromName(this.value);
        swatch.style.backgroundColor = hex || '#fff';
        hiddenHex.value = hex;
    });
    
    // Setup autocomplete
    setTimeout(() => {
        setupColorAutocomplete(`#color-input-${id}`, `#color-dropdown-${id}`, `#color-hex-${id}`);
    }, 0);
    
    return {
        element: container,
        getValue: () => ({
            name: input.value,
            hex: hiddenHex.value || getColorHexFromName(input.value)
        }),
        setValue: (name) => {
            input.value = name;
            const hex = getColorHexFromName(name);
            hiddenHex.value = hex;
            swatch.style.backgroundColor = hex || '#fff';
        }
    };
}
